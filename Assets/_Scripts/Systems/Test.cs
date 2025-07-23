// using System;
// using _Scripts.Utilities;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
//
// namespace _Scripts.Systems
// {
//     [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
//     public partial struct CellInstantiationFromSupernovaSystem : ISystem
//     {
//         private NativeQueue<Entity> _cellPoolQueue;
//
//         public void OnCreate(ref SystemState state)
//         {
//             state.Enabled = false;
//         }
//
//         public void OnUpdate(ref SystemState state)
//         {
//             try
//             {
//                 // 获取全局数据容器引用
//                 // if (_cellPoolQueue.IsCreated) return;
//                 var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
//                 _cellPoolQueue = globalDataSystem.CellPoolQueue;
//
//                 var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
//
//                 if (_cellPoolQueue.TryDequeue(out var cell01))
//                 {
//                     ecb.AddComponent<LocalTransform>(cell01);
//                 }
//
//                 if (_cellPoolQueue.TryDequeue(out var cell02))
//                 {
//                     ecb.AddComponent<LocalTransform>(cell02);
//                 }
//
//                 if (_cellPoolQueue.TryDequeue(out var cell03))
//                 {
//                     ecb.AddComponent<LocalTransform>(cell03);
//                 }
//
//                 ecb.Playback(state.EntityManager);
//                 state.Enabled = false;
//                 Debug.Log("[CellInstantiationFromSupernovaSystem] 已执行，已从 CellPoolQueue 中实例化 3 个 Cell。");
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//                 state.Enabled = false;
//                 throw;
//             }
//         }
//     }
// }