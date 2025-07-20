using _Scripts.Aspects;
using _Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    #region Custom System Groups

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializationCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    #endregion

    #region GlobalDataSystem

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup), OrderFirst = true)]
    public partial class GlobalDataSystem : SystemBase
    {
        public NativeHashMap<int3, Entity> CellMap;
        public NativeQueue<int3> PendingCellsToInstantiate;

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(4096, Allocator.Persistent);
            PendingCellsToInstantiate = new NativeQueue<int3>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (CellMap.IsCreated) CellMap.Dispose();
            if (PendingCellsToInstantiate.IsCreated) PendingCellsToInstantiate.Dispose();
        }
    }

    #endregion

    #region InitializationCellularAutomataSystemGroup

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup))]
    public partial struct CellInstantiationSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<int3> _pendingCells;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellPrototypeTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取 GlobalData
            if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
                _pendingCells = globalDataSystem.PendingCellsToInstantiate;
            }

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var prototype = SystemAPI.GetSingletonEntity<CellPrototypeTag>();

            var instantiateJob = new ContinuousInstantiateCellsJob
            {
                ECB = ecb,
                CellMap = _cellMap,
                PendingCells = _pendingCells,
                Prototype = prototype
            };

            state.Dependency = instantiateJob.Schedule(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        private struct ContinuousInstantiateCellsJob : IJob
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<int3> PendingCells;
            [ReadOnly] public Entity Prototype;

            public void Execute()
            {
                const int maxCellsPerFrame = 512;
                var processedCount = 0;

                while (PendingCells.TryDequeue(out var instantiatePosition) && processedCount < maxCellsPerFrame)
                {
                    // 检查位置是否已被占用
                    if (CellMap.ContainsKey(instantiatePosition)) continue;

                    // 从 Prototype 复制并实例化 Cell
                    var cell = ECB.Instantiate(Prototype);
                    ECB.RemoveComponent<CellPrototypeTag>(cell);
                    ECB.SetComponent(cell,
                        new LocalToWorld
                        {
                            Value = float4x4.TRS(instantiatePosition,
                                quaternion.identity,
                                new float3(0.5f, 0.5f, 0.5f))
                        });

                    // 把 Cell 实体添加到 CellMap 中
                    CellMap.TryAdd(instantiatePosition, cell);
                    processedCount++;
                }
            }
        }
    }

    #endregion

    #region VariableRateCellularAutomataSystemGroup

    [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
    public partial struct CellGenerationFromSupernovaSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<int3> _pendingCells;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取 GlobalData
            if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
                _pendingCells = globalDataSystem.PendingCellsToInstantiate;
            }

            // 收集需要实例化的 Cell 位置
            var collectJob = new CollectCellPositionsToGenerateJob
            {
                PendingCells = _pendingCells.AsParallelWriter()
            };

            state.Dependency = collectJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(ShouldInitializeCell))]
        private partial struct CollectCellPositionsToGenerateJob : IJobEntity
        {
            public NativeQueue<int3>.ParallelWriter PendingCells;

            private void Execute(SupernovaAspect supernova)
            {
                var random = new Random(math.hash(supernova.Position));

                for (var x = -supernova.GenerateRange; x <= supernova.GenerateRange; ++x)
                for (var y = -supernova.GenerateRange; y <= supernova.GenerateRange; ++y)
                for (var z = -supernova.GenerateRange; z <= supernova.GenerateRange; ++z)
                {
                    var offset = new int3(x, y, z);
                    var generatePosition = supernova.Position + offset;

                    if (random.NextFloat(0, 100f) >= supernova.GenerateDensity) continue; // 根据密度决定是否生成 Cell
                    if (math.length(offset) >= supernova.GenerateRange) continue; // 只保留球体内的点

                    PendingCells.Enqueue(generatePosition);
                }
            }
        }
    }

    #endregion
}

// using _Scripts.Aspects;
// using _Scripts.Components;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace _Scripts.Systems
// {
//     #region System Groups and Structs
//
//     public struct PendingCellInitiationData
//     {
//         public int3 Position;
//         public Entity Prefab;
//     }
//
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     public partial class InitializationCellularAutomataSystemGroup : ComponentSystemGroup
//     {
//     }
//
//     [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
//     public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
//     {
//     }
//
//     #endregion
//
//     [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup), OrderFirst = true)]
//     public partial class GlobalDataSystem : SystemBase
//     {
//         public NativeParallelHashMap<int3, Entity> CellMap;
//         public NativeQueue<PendingCellInitiationData> PendingCellsToInstantiate;
//
//         protected override void OnCreate()
//         {
//             CellMap = new NativeParallelHashMap<int3, Entity>(4096, Allocator.Persistent);
//             PendingCellsToInstantiate = new NativeQueue<PendingCellInitiationData>(Allocator.Persistent);
//         }
//
//         protected override void OnUpdate()
//         {
//             Enabled = false;
//         }
//
//         protected override void OnDestroy()
//         {
//             if (CellMap.IsCreated) CellMap.Dispose();
//             if (PendingCellsToInstantiate.IsCreated) PendingCellsToInstantiate.Dispose();
//         }
//     }
//
//     [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup))]
//     public partial struct CellInstantiationSystem : ISystem
//     {
//         private NativeParallelHashMap<int3, Entity> _cellMap;
//         private NativeQueue<PendingCellInitiationData> _pendingCells;
//
//         public void OnUpdate(ref SystemState state)
//         {
//             // 获取全局 CellMap 和 PendingCells
//             if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
//             {
//                 var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
//                 _cellMap = globalDataSystem.CellMap;
//                 _pendingCells = globalDataSystem.PendingCellsToInstantiate;
//             }
//
//             // 如果没有待处理的Cell，跳过
//             if (_pendingCells.Count == 0) return;
//
//             var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
//             var instantiateJob = new ContinuousInstantiateCellsJob
//             {
//                 PendingCells = _pendingCells,
//                 CellMap = _cellMap,
//                 ECB = ecb
//             };
//
//             state.Dependency = instantiateJob.Schedule(state.Dependency);
//             state.Dependency.Complete();
//
//             ecb.Playback(state.EntityManager);
//         }
//
//         [BurstCompile]
//         private struct ContinuousInstantiateCellsJob : IJob
//         {
//             public NativeQueue<PendingCellInitiationData> PendingCells;
//             [NativeDisableParallelForRestriction] public NativeParallelHashMap<int3, Entity> CellMap;
//             public EntityCommandBuffer ECB;
//
//             public void Execute()
//             {
//                 // 自定义每帧处理的 Cell 数量
//                 const int maxCellsPerFrame = 512;
//                 var processedCount = 0;
//
//                 while (PendingCells.TryDequeue(out var cellData) && processedCount < maxCellsPerFrame)
//                 {
//                     // 再次检查位置是否已被占用
//                     if (CellMap.ContainsKey(cellData.Position))
//                     {
//                         processedCount++;
//                         continue;
//                     }
//
//                     // 实例化 Cell 实体
//                     var cell = ECB.Instantiate(cellData.Prefab);
//                     ECB.SetComponent(cell, new LocalTransform
//                     {
//                         Position = cellData.Position,
//                         Rotation = quaternion.identity,
//                         Scale = 1f
//                     });
//
//                     // 把 Cell 实体添加到 CellMap 中
//                     CellMap.TryAdd(cellData.Position, cell);
//                     processedCount++;
//                 }
//             }
//         }
//     }
//
// [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
// public partial struct CellGenerationFromSupernovaSystem : ISystem
// {
//     private NativeParallelHashMap<int3, Entity> _cellMap;
//     private NativeQueue<PendingCellInitiationData> _pendingCells;
//
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<ShouldInitializeCell>();
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         // 获取全局 CellMap 和 PendingCells
//         if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
//         {
//             var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
//             _cellMap = globalDataSystem.CellMap;
//             _pendingCells = globalDataSystem.PendingCellsToInstantiate;
//         }
//
//         // 收集需要实例化的 Cell 位置
//         var collectJob = new CollectCellPositionsToGenerateJob
//         {
//             CellMap = _cellMap,
//             PendingCells = _pendingCells.AsParallelWriter()
//         };
//
//         state.Dependency = collectJob.ScheduleParallel(state.Dependency);
//         state.Dependency.Complete();
//
//         // 因为收集工作完成，禁用 ShouldInitializeCell 组件
//         var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
//         foreach (var (_, entity) in SystemAPI.Query<RefRO<ShouldInitializeCell>>().WithEntityAccess())
//         {
//             ecb.SetComponentEnabled<ShouldInitializeCell>(entity, false);
//         }
//
//         ecb.Playback(state.EntityManager);
//     }
//
//     [BurstCompile]
//     [WithAll(typeof(ShouldInitializeCell))]
//     private partial struct CollectCellPositionsToGenerateJob : IJobEntity
//     {
//         [ReadOnly] public NativeParallelHashMap<int3, Entity> CellMap;
//         public NativeQueue<PendingCellInitiationData>.ParallelWriter PendingCells;
//
//         private void Execute(SupernovaAspect generator)
//         {
//             var center = generator.Position;
//             var range = generator.GenerateRange;
//             var density = generator.GenerateDensity;
//             var prefabs = generator.Prefabs;
//
//             // 计算总权重
//             var totalWeight = 0;
//             for (var i = 0; i < prefabs.Length; i++) totalWeight += prefabs[i].Weight;
//
//             // 获取随机种子，与生成中心的位置相关
//             var random = new Random(math.hash(center));
//
//             for (var x = -range; x <= range; ++x)
//             for (var y = -range; y <= range; ++y)
//             for (var z = -range; z <= range; ++z)
//             {
//                 var pos = center + new int3(x, y, z);
//
//                 // 根据密度决定是否生成 Cell
//                 if (random.NextInt(0, 100) >= density) continue;
//                 // 如果 CellMap 中已经存在这个位置的 Cell，则跳过
//                 if (CellMap.ContainsKey(pos)) continue;
//
//                 // 根据 Cell 权重随机选择 Prefab
//                 var pick = random.NextInt(0, totalWeight);
//                 var acc = 0;
//                 var chosenPrefab = Entity.Null;
//                 for (var i = 0; i < prefabs.Length; i++)
//                 {
//                     acc += prefabs[i].Weight;
//                     if (pick >= acc) continue;
//                     chosenPrefab = prefabs[i].Prefab;
//                     break;
//                 }
//
//                 // 如果没有选择到 Prefab，则跳过
//                 if (chosenPrefab == Entity.Null) continue;
//
//                 // 添加到待实例化队列
//                 PendingCells.Enqueue(new PendingCellInitiationData
//                 {
//                     Position = pos,
//                     Prefab = chosenPrefab
//                 });
//             }
//         }
//     }
// }
// }