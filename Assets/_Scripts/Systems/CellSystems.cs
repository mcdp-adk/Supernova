// using _Scripts.Aspects;
// using _Scripts.Components;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
//
// namespace _Scripts.Systems
// {
//     [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
//     public partial struct GravitySystem : ISystem
//     {
//         private NativeParallelHashMap<int3, Entity> _cellMap;
//         private NativeQueue<PendingCellInitiationData> _pendingCells;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<CellTag>();
//         }
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
//             // 查询所有 Supernova 的 Position
//             var supernovaPositions = new NativeList<int3>(Allocator.TempJob);
//             foreach (var supernova in SystemAPI.Query<SupernovaAspect>()) supernovaPositions.Add(supernova.Position);
//
//             var calculateGravityJob = new CalculateGravityJob
//             {
//                 CellMap = _cellMap,
//                 PendingCells = _pendingCells.AsParallelWriter(),
//                 SupernovaPositions = supernovaPositions.AsArray()
//             };
//
//             state.Dependency = calculateGravityJob.ScheduleParallel(state.Dependency);
//             state.Dependency.Complete();
//         }
//
//         [BurstCompile]
//         [WithAll(typeof(IsCellAlive))]
//         private partial struct CalculateGravityJob : IJobEntity
//         {
//             [ReadOnly] public NativeParallelHashMap<int3, Entity> CellMap;
//             [ReadOnly] public NativeArray<int3> SupernovaPositions;
//             public NativeQueue<PendingCellInitiationData>.ParallelWriter PendingCells;
//
//             private void Execute(CellAspect cell)
//             {
//             }
//         }
//     }
// }