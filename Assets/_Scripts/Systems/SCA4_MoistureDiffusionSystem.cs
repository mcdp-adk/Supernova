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
    [UpdateAfter(typeof(HeatTransferSystem))]
    public partial struct MoistureDiffusionSystem : ISystem
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
            state.Dependency = new MoistureDiffusionJob
            {
                ECB = ecb.AsParallelWriter(),
                CellMap = _cellMap,
                CellConfigs = _cellConfigs,
                TypeLookup = SystemAPI.GetComponentLookup<CellType>(true),
                MoistureLookup = SystemAPI.GetComponentLookup<Moisture>(true)
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);

            state.Dependency = new MoistureUpdateJob().ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct MoistureDiffusionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public NativeArray<CellConfig> CellConfigs;
            [ReadOnly] public ComponentLookup<CellType> TypeLookup;
            [ReadOnly] public ComponentLookup<Moisture> MoistureLookup;

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
                // 检查实体是否存在且组件有效
                if (!TypeLookup.HasComponent(selfEntity) || !MoistureLookup.HasComponent(selfEntity)) return;

                var selfCoordinate = (int3)transform.Position;
                var selfType = TypeLookup[selfEntity];
                var selfMoisture = MoistureLookup[selfEntity];
                var selfMoistureConductivity = CellConfigs.GetCellConfig(selfType.Value).MoistureConductivity;

                var totalMoistureTransfer = 0f;

                foreach (var offset in Directions)
                {
                    var neighborCoordinate = selfCoordinate + offset;
                    if (!CellMap.TryGetValue(neighborCoordinate, out var neighborEntity)) continue;

                    // 检查邻居实体是否存在且组件有效
                    if (!TypeLookup.HasComponent(neighborEntity) ||
                        !MoistureLookup.HasComponent(neighborEntity)) continue;

                    var neighborType = TypeLookup[neighborEntity];
                    var neighborMoisture = MoistureLookup[neighborEntity];

                    // 计算水分扩散
                    var neighborMoistureConductivity =
                        CellConfigs.GetCellConfig(neighborType.Value).MoistureConductivity;
                    var avgConductivity = (selfMoistureConductivity + neighborMoistureConductivity) * 0.5f;
                    var moistureDiff = selfMoisture.Value - neighborMoisture.Value;
                    var moistureTransfer = moistureDiff * avgConductivity * GlobalConfig.MoistureDiffusionCoefficient;

                    if (moistureTransfer == 0f) continue;
                    totalMoistureTransfer -= moistureTransfer;

                    // 将水分扩散给邻居
                    ECB.AppendToBuffer(index, neighborEntity, new MoistureBuffer { Value = moistureTransfer });
                }

                // 将水分变化写入当前实体的 MoistureBuffer
                if (totalMoistureTransfer != 0f)
                    ECB.AppendToBuffer(index, selfEntity, new MoistureBuffer { Value = totalMoistureTransfer });
            }
        }
    }
}