// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
//
// namespace _Scripts.Systems
// {
//     [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup), OrderFirst = true)]
//     public partial struct CellUpdateSystem : ISystem
//     {
//         private NativeHashMap<int3, Entity> _frontCellMap;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//         }
//
//         public void OnUpdate(ref SystemState state)
//         {
//             // 获取全局数据容器引用
//             var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
//             _frontCellMap = globalDataSystem.FrontCellMap;
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//         }
//     }
// }