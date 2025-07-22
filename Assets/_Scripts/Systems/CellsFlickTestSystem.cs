// using _Scripts.Aspects;
// using _Scripts.Components;
// using _Scripts.Utilities;
// using Unity.Burst;
// using Unity.Entities;
//
// namespace _Scripts.Systems
// {
//     [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
//     public partial struct CellsFlickTestSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             foreach (var cell in SystemAPI.Query<CellAspect>().WithAll<IsCellAlive>())
//             {
//                 cell.CellType = cell.CellType switch
//                 {
//                     CellTypeEnum.Cell1 => CellTypeEnum.Cell2,
//                     CellTypeEnum.Cell2 => CellTypeEnum.Cell1,
//                     _ => cell.CellType
//                 };
//             }
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//         }
//     }
// }