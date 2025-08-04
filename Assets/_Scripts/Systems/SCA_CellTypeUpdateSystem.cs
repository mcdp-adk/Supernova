using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup), OrderLast = true)]
    public partial struct CellTypeUpdateSystem : ISystem
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
            state.Dependency = new CellTypeUpdateJob
            {
                ECB = ecb,
                CellMap = _cellMap,
                CellConfigs = _cellConfigs
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct CellTypeUpdateJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeArray<CellConfig> CellConfigs;

            private void Execute(Entity cell, in LocalTransform transform,
                in Temperature temperature, in Moisture moisture)
            {
                var targetType = CellTypeEnum.None;
                foreach (var config in CellConfigs)
                {
                    if (!(temperature.Value >= config.TemperatureMin) ||
                        !(temperature.Value <= config.TemperatureMax) ||
                        !(moisture.Value >= config.MoistureMin) ||
                        !(moisture.Value <= config.MoistureMax)) continue;
                    targetType = config.Type;
                    break;
                }

                CellUtility.SetCellType(cell, ECB, CellMap, (int3)transform.Position, targetType);
            }
        }
    }
}