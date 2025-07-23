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
        #region Initialize Preperations

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

        #endregion

        public static bool TryAddCellToWorld(Entity cell, EntityCommandBuffer ecb,
            NativeHashMap<int3, Entity> cellMap, CellTypeEnum cellType, int3 targetCoordinate)
        {
            if (!cellMap.TryAdd(targetCoordinate, cell)) return false;

            ecb.AddComponent<LocalTransform>(cell);
            ecb.SetComponent(cell, new LocalTransform
            {
                Position = targetCoordinate,
                Rotation = quaternion.identity,
                Scale = GlobalConfig.DefaultCellScale
            });

            SetCellType(cell, ecb, cellType);

            return true;
        }

        public static bool TryMoveCell(Entity cell, EntityManager manager,
            NativeHashMap<int3, Entity> cellMap, int3 targetCoordinate)
        {
            if (!cellMap.TryAdd(targetCoordinate, cell)) return false;

            cellMap.Remove((int3)manager.GetComponentData<LocalTransform>(cell).Position);
            manager.SetComponentData(cell, new LocalTransform
            {
                Position = targetCoordinate,
                Rotation = quaternion.identity,
                Scale = GlobalConfig.DefaultCellScale
            });

            return true;
        }

        #region SetCellType

        public static void SetCellType(Entity cell, EntityManager manager, CellTypeEnum targetCellType)
        {
            manager.SetComponentEnabled<IsCellAlive>(cell, targetCellType != CellTypeEnum.None);

            manager.SetComponentData(cell, new CellType { Value = targetCellType });
            manager.SetComponentData(cell, new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                MeshID = new BatchMeshID { value = (uint)targetCellType }
            });
        }

        public static void SetCellType(Entity cell, EntityCommandBuffer ecb, CellTypeEnum targetCellType)
        {
            ecb.SetComponentEnabled<IsCellAlive>(cell, targetCellType != CellTypeEnum.None);
            ecb.SetComponent(cell, new CellType { Value = targetCellType });
            ecb.SetComponent(cell, new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                MeshID = new BatchMeshID { value = (uint)targetCellType }
            });
        }

        #endregion
    }
}