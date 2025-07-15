using _Scripts.Aspects;
using _Scripts.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GenerateCellSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var generator in SystemAPI.Query<CellGeneratorAspect>().WithAll<ShouldInitializeCell>())
            {
                var center = generator.Position;
                var range = generator.CoreRange;
                var prefab = generator.CellPrefab;

                for (var x = -range; x <= range; x++)
                for (var y = -range; y <= range; y++)
                for (var z = -range; z <= range; z++)
                {
                    var pos = center + new int3(x, y, z);
                    var cell = ecb.Instantiate(prefab);
                    ecb.SetComponent(cell, new LocalTransform
                    {
                        Position = pos,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                }

                ecb.SetComponentEnabled<ShouldInitializeCell>(generator.Self, false);
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}