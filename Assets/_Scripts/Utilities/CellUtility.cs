using _Scripts.Components;
using Unity.Entities;
using Unity.Rendering;

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
            manager.AddComponent<CellType>(prototype);
            manager.AddComponent<IsCellAlive>(prototype);
            manager.SetComponentEnabled<IsCellAlive>(prototype, false);
            manager.AddBuffer<PendingCellUpdateBuffer>(prototype);
        }

        public static Entity InitiateFromPrototype(Entity prototype, EntityCommandBuffer ecb)
        {
            var cell = ecb.Instantiate(prototype);

            ecb.SetName(cell, "");
            ecb.RemoveComponent<CellPrototypeTag>(cell);
            ecb.SetComponentEnabled<IsCellAlive>(cell, false);

            return cell;
        }

        public static void AddCellToWorldFromQueue(Entity cell, EntityCommandBuffer ecb)
        {
        }
        
        public static void SetCellType(Entity cell, CellTypeEnum targetCellType, EntityManager manager)
        {
            
        }
    }
}