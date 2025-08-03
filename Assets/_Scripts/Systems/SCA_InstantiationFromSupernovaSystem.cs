using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup))]
    public partial struct InstantiationFromSupernovaSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellConfigTag>();
        }

        // ========== 全局数据引用 ==========
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<Entity> _cellPoolQueue;

        // ========== 系统生命周期 ==========
        public void OnUpdate(ref SystemState state)
        {
            // 初始化 CellMap
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            // 初始化 CellPoolQueue
            if (!_cellPoolQueue.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellPoolQueue = globalDataSystem.CellPoolQueue;
            }

            // 开启 Cell 实例化 Job
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var instantiateCellJob = new InstantiateCellJob
            {
                Manager = state.EntityManager,
                ECB = ecb,
                CellMap = _cellMap,
                CellPoolQueue = _cellPoolQueue,
                ConfigEntity = SystemAPI.GetSingletonEntity<CellConfigTag>()
            };

            // 等待 Job 完成后回放 Entity 修改
            state.Dependency = instantiateCellJob.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
        }

        // ========== 实例化 Cell 作业 ==========

        [BurstCompile]
        [WithAll(typeof(ShouldInitializeCell))]
        private partial struct InstantiateCellJob : IJobEntity
        {
            public EntityManager Manager;
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<Entity> CellPoolQueue;
            public Entity ConfigEntity;

            private void Execute(SupernovaAspect supernova,
                EnabledRefRW<ShouldInitializeCell> shouldInitializeCell)
            {
                var center = supernova.Coordinate;
                var random = new Random(math.hash(center));
                var maxRadius = supernova.MaxRadius;

                for (var x = -maxRadius; x <= maxRadius; x++)
                for (var y = -maxRadius; y <= maxRadius; y++)
                for (var z = -maxRadius; z <= maxRadius; z++)
                {
                    var offset = new int3(x, y, z);
                    var distance = math.length(offset);
                    
                    var (layerIndex, layerConfig) = supernova.GetLayerForDistance(distance);
                    if (layerIndex < 0) continue;

                    if (random.NextFloat(0f, 100f) > layerConfig.Density) continue;

                    var targetCoordinate = center + offset;

                    var direction = math.normalizesafe(offset);
                    var angle = random.NextFloat(-layerConfig.ExplosionAngleClamp, layerConfig.ExplosionAngleClamp);
                    var rotation = quaternion.AxisAngle(math.up(), math.radians(angle));
                    var finalDirection = math.mul(rotation, direction);
                    var initialImpulse = finalDirection * layerConfig.ExplosionStrength;

                    if (CellPoolQueue.TryDequeue(out var cell))
                    {
                        CellUtility.TryAddCellToWorld(
                            cell, Manager, ECB, CellMap, ConfigEntity,
                            supernova.GetRandomCellType(random, layerIndex), targetCoordinate, initialImpulse);
                    }
                    else return;
                }

                shouldInitializeCell.ValueRW = false;
            }
        }
    }
}