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
    public partial struct CellInstantiationFromSupernovaSystem : ISystem
    {
        // ========== 全局数据引用 ==========
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<Entity> _cellPoolQueue;

        // ========== 系统生命周期 ==========
        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据容器引用
            if (_cellMap.IsCreated && _cellPoolQueue.IsCreated) return;
            var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
            _cellMap = globalDataSystem.CellMap;
            _cellPoolQueue = globalDataSystem.CellPoolQueue;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var instantiateCellJob = new InstantiateCellJob
            {
                ECB = ecb,
                CellMap = _cellMap,
                CellPoolQueue = _cellPoolQueue
            };

            state.Dependency = instantiateCellJob.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        private partial struct InstantiateCellJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<Entity> CellPoolQueue;

            private void Execute(SupernovaAspect supernova, EnabledRefRW<ShouldInitializeCell> shouldInitializeCell)
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

                    // 检查是否在球形范围内
                    if (math.lengthsq(offset) > rangeSquared) continue;

                    // 根据密度随机决定是否生成 Cell
                    if (random.NextFloat(0f, 100f) > density) continue;

                    // 尝试从 Cell 池中获取 Cell 并添加到世界
                    if (CellPoolQueue.TryDequeue(out var cell))
                    {
                        CellUtility.TryAddCellToWorldFromCellPoolQueue(
                            cell, ECB, CellMap,
                            targetCoordinate, supernova.GetRandomCellType(random));
                    }
                    else return;
                }

                shouldInitializeCell.ValueRW = false;
            }
        }
    }
}

// using _Scripts.Components;
// using _Scripts.Utilities;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Rendering;
// using Unity.Transforms;
// using UnityEngine.Rendering;
//
// namespace _Scripts.Systems
// {
//     /// <summary>
//     /// Cell 实例化系统 - 负责将待生成的 Cell 数据转换为实际的 Entity
//     /// 采用分帧实例化策略避免单帧生成过多 Entity 导致性能问题
//     /// </summary>
//     [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
//     [UpdateAfter(typeof(GlobalDataSystem))]
//     public partial struct CellInstantiationSystem : ISystem
//     {
//         // ========== 全局数据引用 ==========
//         private NativeHashMap<int3, Entity> _cellMap;
//         private NativeQueue<PendingCellData> _pendingCells;
//
//         // ========== 系统生命周期 ==========
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<CellPrototypeTag>();
//             state.Enabled = false;
//         }
//
//         public void OnUpdate(ref SystemState state)
//         {
//             // 获取全局数据容器引用
//             if (_cellMap.IsCreated && _pendingCells.IsCreated) return;
//             var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
//             _cellMap = globalDataSystem.CellMap;
//             _pendingCells = globalDataSystem.PendingCellsToInstantiate;
//
//             // 获取 Cell 原型实体
//             var prototype = SystemAPI.GetSingletonEntity<CellPrototypeTag>();
//             var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
//
//             // 执行连续实例化作业
//             var instantiateJob = new ContinuousInstantiateCellsJob
//             {
//                 ECB = ecb,
//                 CellMap = _cellMap,
//                 PendingCells = _pendingCells,
//                 Prototype = prototype
//             };
//
//             state.Dependency = instantiateJob.Schedule(state.Dependency);
//             state.Dependency.Complete();
//
//             ecb.Playback(state.EntityManager);
//         }
//
//         // ========== 作业定义 ==========
//
//         /// <summary>
//         /// 连续实例化 Cell 作业
//         /// 每帧处理固定数量的待生成 Cell，确保性能稳定
//         /// </summary>
//         [BurstCompile]
//         private struct ContinuousInstantiateCellsJob : IJob
//         {
//             public EntityCommandBuffer ECB;
//             public NativeHashMap<int3, Entity> CellMap;
//             public NativeQueue<PendingCellData> PendingCells;
//             [ReadOnly] public Entity Prototype;
//
//             public void Execute()
//             {
//                 var processedCount = 0;
//
//                 // 分帧处理待生成的 Cell
//                 while (PendingCells.TryDequeue(out var pendingCellData) &&
//                        processedCount < GlobalConfig.MaxCellsPerFrame)
//                 {
//                     // 检查位置冲突
//                     if (CellMap.ContainsKey(pendingCellData.Coordinate))
//                         continue;
//
//                     // 实例化并配置 Cell
//                     var cell = CreateCellFromPrototype(pendingCellData);
//
//                     // 注册到全局映射表
//                     CellMap.TryAdd(pendingCellData.Coordinate, cell);
//                     processedCount++;
//                 }
//             }
//