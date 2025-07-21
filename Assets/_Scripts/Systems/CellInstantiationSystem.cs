using _Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace _Scripts.Systems
{
    public struct PendingCellData
    {
        public int3 Position;
        public CellTypeEnum CellType;
    }

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup))]
    public partial struct CellInstantiationSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

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

            // 持续实例化 Cell
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
            public NativeQueue<PendingCellData> PendingCells;
            [ReadOnly] public Entity Prototype;

            public void Execute()
            {
                const int maxCellsPerFrame = 512;
                var processedCount = 0;

                while (PendingCells.TryDequeue(out var pendingCellData) && processedCount < maxCellsPerFrame)
                {
                    // 检查位置是否已被占用
                    if (CellMap.ContainsKey(pendingCellData.Position)) continue;

                    // 从 Prototype 复制并实例化 Cell
                    var cell = ECB.Instantiate(Prototype);

                    // 设置 Cell 的组件
                    SetCellComponents(cell, pendingCellData);

                    // 把 Cell 实体添加到 CellMap 中
                    CellMap.TryAdd(pendingCellData.Position, cell);
                    processedCount++;
                }
            }

            private void SetCellComponents(Entity cell, PendingCellData pendingCellData)
            {
                // 清除从 CellPrototype 继承的名称
                ECB.SetName(cell, "");
                
                // 删除 CellPrototypeTag 组件并启用 IsCellAlive
                ECB.RemoveComponent<CellPrototypeTag>(cell);
                ECB.SetComponentEnabled<IsCellAlive>(cell, true);

                // 设置 Cell 的位置和 LocalToWorld 组件以渲染
                ECB.SetComponent(cell, new CellPosition { Value = pendingCellData.Position });
                ECB.SetComponent(cell, new LocalToWorld
                {
                    Value = float4x4.TRS(pendingCellData.Position,
                        quaternion.identity,
                        new float3(0.5f, 0.5f, 0.5f))
                });

                // 设置 Cell 类型以及显示的 Mesh 和 Material
                ECB.SetComponent(cell, new CellType { Value = pendingCellData.CellType });
                ECB.SetComponent(cell, new MaterialMeshInfo
                {
                    MaterialID = new BatchMaterialID { value = (uint)pendingCellData.CellType },
                    MeshID = new BatchMeshID { value = (uint)pendingCellData.CellType }
                });
            }
        }
    }
}