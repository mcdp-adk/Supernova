using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
            var maxStep = math.max(1,
                (int)math.floor(GlobalConfig.MaxSpeed * deltaTime * GlobalConfig.PhysicsSpeedScale));

            var step = maxStep;
            while (step > 0)
            {
                // 1. 移动与碰撞
                state.Dependency = new TryMoveCellJob
                {
                    CellMap = _cellMap,
                    MassLookup = SystemAPI.GetComponentLookup<Mass>(true),
                    VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(),
                    ImpulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>()
                }.Schedule(state.Dependency);
                state.Dependency.Complete();

                // 2. 冲量整合与速度更新
                state.Dependency = new VelocityUpdateJob
                {
                    MassLookup = SystemAPI.GetComponentLookup<Mass>(true),
                    VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(),
                    ImpulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>()
                }.Schedule(state.Dependency);
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
            public ComponentLookup<Velocity> VelocityLookup;
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(Entity self, ref LocalTransform localTransform)
            {
                var currentVelocity = VelocityLookup[self].Value;
                var currentMass = MassLookup[self].Value;
                var offset = (int3)math.round(math.normalize(currentVelocity));
                var currentCoordinate = (int3)localTransform.Position;
                var targetCoordinate = currentCoordinate + offset;

                // 如果目标坐标已经有 Cell，尝试处理碰撞
                if (CellUtility.TryMoveCell(self, ref localTransform, CellMap, targetCoordinate)) return;

                var targetEntity = CellMap[targetCoordinate];
                var targetVelocity = VelocityLookup[targetEntity].Value;
                var targetMass = MassLookup[targetEntity].Value;

                var collisionNormal = math.normalize(targetCoordinate - currentCoordinate);
                var relativeSpeed = math.dot(currentVelocity - targetVelocity, collisionNormal);
                var impulseMagnitude = (2 * relativeSpeed) / (currentMass + targetMass);

                // 应用冲量损失
                var adjustedImpulseMagnitude = impulseMagnitude * GlobalConfig.ImpulseLossFactor;
                var currentImpulse = -adjustedImpulseMagnitude * targetMass * collisionNormal;
                var targetImpulse = adjustedImpulseMagnitude * currentMass * collisionNormal;

                ImpulseBufferLookup[self].Add(new ImpulseBuffer { Value = currentImpulse });
                ImpulseBufferLookup[targetEntity].Add(new ImpulseBuffer { Value = targetImpulse });
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct VelocityUpdateJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Mass> MassLookup;
            public ComponentLookup<Velocity> VelocityLookup;
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(Entity cell)
            {
                // 计算总冲量
                var totalImpulse = float3.zero;
                foreach (var impulse in ImpulseBufferLookup[cell]) totalImpulse += impulse.Value;

                // 计算新速度
                var newVelocity = VelocityLookup[cell].Value + totalImpulse / MassLookup[cell].Value;

                // 限制最大速度
                var speedSq = math.lengthsq(newVelocity);
                if (speedSq > GlobalConfig.MaxSpeed * GlobalConfig.MaxSpeed)
                    newVelocity = math.normalize(newVelocity) * GlobalConfig.MaxSpeed;

                // 更新速度
                VelocityLookup[cell] = new Velocity { Value = newVelocity };

                // 根据速度模长，启用/禁用 Velocity 组件
                VelocityLookup.SetComponentEnabled(cell, math.lengthsq(newVelocity) >= 1f);

                // 清空冲量缓冲区
                ImpulseBufferLookup[cell].Clear();
            }
        }
    }
}