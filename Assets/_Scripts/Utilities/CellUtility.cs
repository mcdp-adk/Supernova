using _Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace _Scripts.Utilities
{
    public static class CellUtility
    {
        public static void CreatePrototype(string prototypeName, EntityManager manager,
            RenderMeshDescription description, RenderMeshArray renderMeshArray)
        {
            var prototype = manager.CreateEntity();

            manager.SetName(prototype, prototypeName);

            RenderMeshUtility.AddComponents(
                prototype,
                manager,
                description,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            manager.AddComponent<CellPrototypeTag>(prototype);
            manager.AddComponent<CellTag>(prototype);
            manager.AddComponent<CellPendingDequeue>(prototype);
            manager.SetComponentEnabled<CellPendingDequeue>(prototype, false);
            manager.AddComponent<IsCellAlive>(prototype);
            manager.SetComponentEnabled<IsCellAlive>(prototype, false);
            manager.AddComponent<CellType>(prototype);
            manager.AddBuffer<PendingCellUpdateBuffer>(prototype);
        }

        public static void InstantiateFromPrototype(Entity prototype, EntityCommandBuffer ecb)
        {
            var cell = ecb.Instantiate(prototype);

            ecb.SetName(cell, "");
            ecb.RemoveComponent<CellPrototypeTag>(cell);
            ecb.SetComponentEnabled<CellPendingDequeue>(cell, true);
        }

        public static void EnqueueCellIntoPool(Entity cell, EntityCommandBuffer ecb, NativeQueue<Entity> queue)
        {
            queue.Enqueue(cell);
            ecb.SetComponentEnabled<CellPendingDequeue>(cell, false);
        }

        public static bool TryAddCellToWorldFromCellPoolQueue(Entity cell, EntityCommandBuffer ecb,
            NativeHashMap<int3, Entity> cellMap, int3 targetCoordinate, CellTypeEnum cellType)
        {
            if (!cellMap.TryAdd(targetCoordinate, cell)) return false;
            
            ecb.SetComponentEnabled<IsCellAlive>(cell, true);
            ecb.SetComponent(cell, new CellType { Value = cellType });
            ecb.AddComponent<LocalTransform>(cell);
            ecb.SetComponent(cell, new LocalTransform
            {
                Position = targetCoordinate,
                Rotation = quaternion.identity,
                Scale = GlobalConfig.DefaultCellScale
            });
            ecb.SetComponent(cell, new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)cellType },
                MeshID = new BatchMeshID { value = (uint)cellType }
            });
            
            return true;
        }

        public static void SetCellType(Entity cell, CellTypeEnum targetCellType, EntityManager manager)
        {
        }
    }
}