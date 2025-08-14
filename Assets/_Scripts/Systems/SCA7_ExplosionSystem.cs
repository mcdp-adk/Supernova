using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup))]
    [UpdateAfter(typeof(CombustionSystem))]
    public partial struct ExplosionSystem : ISystem
    {
        private NativeArray<CellConfig> _cellConfigs;
        private NativeHashMap<int3, Entity> _cellMap;

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellConfigs.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellConfigs = globalDataSystem.CellConfigs;
            }

            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            using var ecb1 = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new ExplosionJob
            {
                ECB = ecb1.AsParallelWriter(),
                CellMap = _cellMap,
                ImpulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>(true),
                HeatBufferLookup = SystemAPI.GetBufferLookup<HeatBuffer>(true)
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb1.Playback(state.EntityManager);

            using var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new EnergyCheckJob
            {
                ECB = ecb2,
                CellMap = _cellMap,
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb2.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(ShouldExplosion))]
        private partial struct ExplosionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;
            [ReadOnly] public BufferLookup<HeatBuffer> HeatBufferLookup;

            private void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform,
                ref Energy energy)
            {
                var coordinate = (int3)transform.Position;

                // 爆炸消耗所有能量
                var totalEnergy = energy.Value;
                if (totalEnergy <= 0f) return;

                // 计算释放的热量
                var heatReleased = totalEnergy * GlobalConfig.ExplosionHeatCoefficient;

                // 限制爆炸影响范围，避免性能问题
                var explosionRange = math.min((int)math.ceil(math.sqrt(totalEnergy)), 10); // 最大范围限制为 10

                // 设置自身能量为 0
                energy.Value = 0f;

                // 添加热量到 HeatBuffer
                ECB.AppendToBuffer(index, entity, new HeatBuffer { Value = heatReleased });

                // 使用球形范围而非立方体范围，减少不必要的计算
                for (var dx = -explosionRange; dx <= explosionRange; dx++)
                for (var dy = -explosionRange; dy <= explosionRange; dy++)
                for (var dz = -explosionRange; dz <= explosionRange; dz++)
                {
                    var offset = new int3(dx, dy, dz);
                    var distance = math.length(offset);

                    // 早期跳出：先检查距离，避免不必要的计算
                    if (distance > explosionRange) continue;
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    var targetCoordinate = coordinate + offset;
                    if (!CellMap.TryGetValue(targetCoordinate, out var targetEntity)) continue;

                    // 计算距离衰减因子（线性衰减）
                    var distanceFactor = 1f - (distance / explosionRange);

                    // 计算冲击力
                    var impulseMagnitude =
                        totalEnergy * GlobalConfig.ExplosionImpulseCoefficient * distanceFactor;

                    // 计算冲击方向
                    var direction = math.normalize(offset);
                    var impulse = direction * impulseMagnitude;

                    // 只对有 ImpulseBuffer 的 entity 添加冲击力
                    if (ImpulseBufferLookup.HasBuffer(targetEntity))
                    {
                        ECB.AppendToBuffer(index, targetEntity, new ImpulseBuffer { Value = impulse });
                    }

                    // 只对有 HeatBuffer 的 entity 添加热量
                    if (HeatBufferLookup.HasBuffer(targetEntity))
                    {
                        var heatToTarget = heatReleased * distanceFactor * 0.1f;
                        ECB.AppendToBuffer(index, targetEntity, new HeatBuffer { Value = heatToTarget });
                    }
                }
            }
        }
    }
}