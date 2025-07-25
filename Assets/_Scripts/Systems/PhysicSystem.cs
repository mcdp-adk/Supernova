using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup))]
    public partial struct PhysicSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据容器引用
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            var maxStep = (int)math.floor(GlobalConfig.MaxVelocity * deltaTime);

            // 获取 BufferLookup、ComponentLookup
            var massLookup = SystemAPI.GetComponentLookup<Mass>(true);
            var velocityLookup = SystemAPI.GetComponentLookup<Velocity>(true);
            var impulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>();

            var step = maxStep;
            while (step > 0)
            {
                // 1. 移动与碰撞
                state.Dependency = new TryMoveCellJob
                {
                    CellMap = _cellMap,
                    MassLookup = massLookup,
                    VelocityLookup = velocityLookup,
                    ImpulseBufferLookup = impulseBufferLookup
                }.Schedule(state.Dependency);
                state.Dependency.Complete();

                // 2. 冲量整合与速度更新
                state.Dependency = new VelocityUpdateJob
                {
                    ImpulseBufferLookup = impulseBufferLookup
                }.ScheduleParallel(state.Dependency);
                state.Dependency.Complete();

                // 3. 检查是否还有可移动 Cell
                if (SystemAPI.QueryBuilder().WithAll<IsAlive, Velocity>().Build().CalculateEntityCount() == 0) break;

                // 4. 更新计数
                step--;
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(Velocity))]
        private partial struct TryMoveCellJob : IJobEntity
        {
            public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public ComponentLookup<Mass> MassLookup;
            [ReadOnly] public ComponentLookup<Velocity> VelocityLookup;
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(CellAspect cell)
            {
                var velocity = cell.Velocity.ValueRO.Value;
                var offset = (int3)math.round(math.normalize(velocity));
                var currentCoordinate = (int3)cell.LocalTransform.ValueRO.Position;
                var targetCoordinate = (int3)math.round(currentCoordinate + offset);

                if (CellUtility.TryMoveCell(cell.Self, ref cell.LocalTransform.ValueRW,
                        CellMap, targetCoordinate))
                {
                    // 移动成功，消耗一步速度分量
                    cell.Velocity.ValueRW.Value -= offset;
                }
                else
                {
                    var targetEntity = CellMap[targetCoordinate];
                    var selfVelocity = cell.Velocity.ValueRO.Value;
                    var selfMass = cell.Mass.ValueRO.Value;
                    var targetVelocity = VelocityLookup[targetEntity].Value;
                    var targetMass = MassLookup[targetEntity].Value;
                    var collisionNormal = math.normalize(targetCoordinate - currentCoordinate);
                    var relativeSpeed = math.dot(selfVelocity - targetVelocity, collisionNormal);
                    var impulseMagnitude = (2 * relativeSpeed) / (selfMass + targetMass);
                    var selfImpulse = -impulseMagnitude * targetMass * collisionNormal;
                    var targetImpulse = impulseMagnitude * selfMass * collisionNormal;
                    ImpulseBufferLookup[cell.Self].Add(new ImpulseBuffer { Value = selfImpulse });
                    ImpulseBufferLookup[targetEntity].Add(new ImpulseBuffer { Value = targetImpulse });
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct VelocityUpdateJob : IJobEntity
        {
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(CellAspect cell)
            {
                // 计算总冲量
                var totalImpulse = float3.zero;
                foreach (var impulse in cell.ImpulseBuffer) totalImpulse += impulse.Value;

                // 更新速度
                cell.Velocity.ValueRW.Value += totalImpulse / cell.Mass.ValueRO.Value;
                ImpulseBufferLookup[cell.Self].Clear();

                // 根据速度模长，启用/禁用 Velocity 组件
                cell.VelocityEnabled.ValueRW = math.lengthsq(cell.Velocity.ValueRO.Value) >= 1f;
            }
        }
    }
}