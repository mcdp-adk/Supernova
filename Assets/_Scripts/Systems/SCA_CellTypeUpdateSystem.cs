using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup), OrderLast = true)]
    public partial struct CellTypeUpdateSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<Entity> _cellPoolQueue;
        private NativeArray<CellConfig> _cellConfigs;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellConfigBuffer>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 初始化 CellMap
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            // 初始化 CellPoolQueue
            if (!_cellPoolQueue.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellPoolQueue = globalDataSystem.CellPoolQueue;
            }

            // 初始化 CellConfigs
            if (!_cellConfigs.IsCreated)
            {
                var buffer = SystemAPI.GetSingletonBuffer<CellConfigBuffer>();
                _cellConfigs = new NativeArray<CellConfig>(buffer.Length, Allocator.Persistent);
                for (var i = 0; i < buffer.Length; i++)
                    _cellConfigs[i] = buffer[i].Data;
            }

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new CellTypeUpdateJob
            {
                ECB = ecb,
                CellMap = _cellMap,
                CellPoolQueue = _cellPoolQueue,
                CellConfigs = _cellConfigs
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_cellConfigs.IsCreated) _cellConfigs.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct CellTypeUpdateJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<Entity> CellPoolQueue;
            public NativeArray<CellConfig> CellConfigs;

            private void Execute(Entity cell, in Temperature temperature, in Moisture moisture)
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

                CellUtility.SetCellType(cell, ECB, CellMap, CellPoolQueue, targetType);
            }
        }
    }
}