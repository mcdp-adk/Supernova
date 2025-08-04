using Unity.Collections;
using Unity.Entities;
using _Scripts.Components;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GlobalDataInitSystem))]
    public partial class CellPoolQueueSystem : SystemBase
    {
        private NativeQueue<Entity> _cellPoolQueue;

        protected override void OnCreate()
        {
            RequireForUpdate<CellTag>();
            RequireForUpdate<PendingDequeue>();
        }

        protected override void OnUpdate()
        {
            if (!_cellPoolQueue.IsCreated)
            {
                var globalDataSystem = World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellPoolQueue = globalDataSystem.CellPoolQueue;
            }

            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<CellTag>>()
                         .WithAll<PendingDequeue>()
                         .WithEntityAccess())
            {
                _cellPoolQueue.Enqueue(entity);
                ecb.SetComponentEnabled<PendingDequeue>(entity, false);
            }

            ecb.Playback(EntityManager);
        }
    }
}