using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup), OrderFirst = true)]
    public partial class GlobalDataSystem : SystemBase
    {
        public NativeHashMap<int3, Entity> CellMap;
        public NativeQueue<PendingCellData> PendingCellsToInstantiate;

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(4096, Allocator.Persistent);
            PendingCellsToInstantiate = new NativeQueue<PendingCellData>(Allocator.Persistent);
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
}