using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup))]
    [UpdateAfter(typeof(EvaporationSystem))]
    public partial struct CombustionSystem : ISystem
    {
        private NativeArray<CellConfig> _cellConfigs;

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellConfigs.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellConfigs = globalDataSystem.CellConfigs;
            }

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new CombustionJob
            {
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(IsBurning))]
        private partial struct CombustionJob : IJobEntity
        {
            private void Execute()
            {
            }
        }
    }
}