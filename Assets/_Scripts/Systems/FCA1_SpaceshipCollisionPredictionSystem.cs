using _Scripts.Utilities;
using Unity.Burst;
using Unity.Entities;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup))]
    [UpdateAfter(typeof(SpaceshipVoxelizationSystem))]
    public partial struct SpaceshipCollisionPredictionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}