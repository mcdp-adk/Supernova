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
    [UpdateAfter(typeof(RandomInstantiationSystem))]
    public partial struct HeatTransferSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeArray<CellConfig> _cellConfigs;

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            if (!_cellConfigs.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellConfigs = globalDataSystem.CellConfigs;
            }

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new HeatTransferJob
            {
                ECB = ecb.AsParallelWriter(),
                CellMap = _cellMap,
                CellConfigs = _cellConfigs,
                TypeLookup = SystemAPI.GetComponentLookup<CellType>(true),
                TemperatureLookup = SystemAPI.GetComponentLookup<Temperature>(true)
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);

            state.Dependency = new TemperatureUpdateJob
            {
                CellConfigs = _cellConfigs
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct HeatTransferJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public NativeArray<CellConfig> CellConfigs;
            [ReadOnly] public ComponentLookup<CellType> TypeLookup;
            [ReadOnly] public ComponentLookup<Temperature> TemperatureLookup;

            private static readonly int3[] Directions =
            {
                new(1, 0, 0), // 右
                new(-1, 0, 0), // 左
                new(0, 1, 0), // 上
                new(0, -1, 0), // 下
                new(0, 0, 1), // 前
                new(0, 0, -1) // 后
            };

            private void Execute([EntityIndexInQuery] int index, Entity selfEntity, in LocalTransform transform)
            {
                var selfCoordinate = (int3)transform.Position;
                var selfType = TypeLookup[selfEntity];
                var selfTemperature = TemperatureLookup[selfEntity];
                var selfHeatConductivity = CellConfigs.GetCellConfig(selfType.Value).HeatConductivity;

                var totalHeatTransfer = 0f;

                foreach (var offset in Directions)
                {
                    var neighborCoordinate = selfCoordinate + offset;
                    if (!CellMap.TryGetValue(neighborCoordinate, out var neighborEntity)) continue;
                    var neighborType = TypeLookup[neighborEntity];
                    var neighborTemperature = TemperatureLookup[neighborEntity];

                    // 计算热量传递
                    var neighborHeatConductivity = CellConfigs.GetCellConfig(neighborType.Value).HeatConductivity;
                    var avgConductivity = (selfHeatConductivity + neighborHeatConductivity) * 0.5f;
                    var tempDiff = selfTemperature.Value - neighborTemperature.Value;
                    var heatTransfer = tempDiff * avgConductivity * GlobalConfig.HeatTransferCoefficient;

                    if (heatTransfer == 0f) continue; // 如果没有热量传递则跳过
                    totalHeatTransfer -= heatTransfer;

                    // 使用 ECB 向邻居的 HeatBuffer 添加热量
                    ECB.AppendToBuffer(index, neighborEntity, new HeatBuffer { Value = heatTransfer });
                }

                // 使用 ECB 向当前实体的 HeatBuffer 添加总热量变化
                if (totalHeatTransfer != 0f)
                    ECB.AppendToBuffer(index, selfEntity, new HeatBuffer { Value = totalHeatTransfer });
            }
        }
    }
}