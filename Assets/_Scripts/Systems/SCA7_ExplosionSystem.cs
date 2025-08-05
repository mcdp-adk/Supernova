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

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new ExplosionJob
            {
                ECB = ecb.AsParallelWriter(),
                CellMap = _cellMap,
                EnergyLookup = SystemAPI.GetComponentLookup<Energy>(),
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(ShouldExplosion))]
        private partial struct ExplosionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public ComponentLookup<Energy> EnergyLookup;

            private void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform)
            {
                var energy = EnergyLookup[entity];
                var coordinate = (int3)transform.Position;

                // 爆炸消耗所有能量
                var totalEnergy = energy.Value;
                if (totalEnergy <= 0f) return;

                // 计算释放的热量
                var heatReleased = totalEnergy * GlobalConfig.ExplosionHeatCoefficient;

                // 计算爆炸影响范围（与能量正相关）
                var explosionRange = (int)math.ceil(math.sqrt(totalEnergy));

                // 设置自身能量为 0
                ECB.SetComponent(index, entity, new Energy { Value = 0f });

                // 添加热量到 HeatBuffer
                ECB.AppendToBuffer(index, entity, new HeatBuffer { Value = heatReleased });

                // 处理爆炸范围内的所有 Cell
                for (var dx = -explosionRange; dx <= explosionRange; dx++)
                for (var dy = -explosionRange; dy <= explosionRange; dy++)
                for (var dz = -explosionRange; dz <= explosionRange; dz++)
                {
                    var offset = new int3(dx, dy, dz);
                    var distance = math.length(offset);

                    // 跳过超出范围和自身的 Cell
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

                    // 添加冲击力到 ImpulseBuffer
                    ECB.AppendToBuffer(index, targetEntity, new ImpulseBuffer { Value = impulse });

                    // 添加热量到 HeatBuffer（距离衰减）
                    var heatToTarget = heatReleased * distanceFactor * 0.1f; // 减少传递的热量
                    ECB.AppendToBuffer(index, targetEntity, new HeatBuffer { Value = heatToTarget });
                }


                // 爆炸后转换为 None 类型
                CellUtility.SetCellTypeToNone(index, entity, ECB, CellMap, coordinate);
            }
        }
    }
}