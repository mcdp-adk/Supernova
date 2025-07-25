using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
    public partial struct InstantiationFromSupernovaSystem : ISystem
    {
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
                ECB = ecb,
                CellMap = _cellMap,
                CellPoolQueue = _cellPoolQueue
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
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<Entity> CellPoolQueue;

            private void Execute(SupernovaAspect supernova,
                EnabledRefRW<ShouldInitializeCell> shouldInitializeCell)
            {
                var center = supernova.Coordinate;
                var range = supernova.GenerateRange;
                var rangeSquared = range * range;
                var density = supernova.GenerateDensity;
                var random = new Random(math.hash(center));

                // 在生成范围内随机生成cell
                for (var x = -range; x <= range; x++)
                for (var y = -range; y <= range; y++)
                for (var z = -range; z <= range; z++)
                {
                    var offset = new int3(x, y, z);
                    var targetCoordinate = center + offset;

                    // 根据密度随机决定是否生成 Cell
                    if (random.NextFloat(0f, 100f) > density) continue;

                    // 检查是否在球形范围内
                    if (math.lengthsq(offset) > rangeSquared) continue;

                    // 尝试从 Cell 池中获取 Cell 并添加到世界
                    if (CellPoolQueue.TryDequeue(out var cell))
                    {
                        CellUtility.TryAddCellToWorld(
                            cell, ECB, CellMap,
                            supernova.GetRandomCellType(random), targetCoordinate);
                    }
                    else return;
                }

                shouldInitializeCell.ValueRW = false;
            }
        }
    }
}