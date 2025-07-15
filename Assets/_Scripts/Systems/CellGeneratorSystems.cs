using _Scripts.Aspects;
using _Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CellMapSystem : SystemBase
    {
        public NativeParallelHashMap<int3, Entity> CellMap;

        protected override void OnCreate()
        {
            CellMap = new NativeParallelHashMap<int3, Entity>(1024, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (CellMap.IsCreated) CellMap.Dispose();
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(CellMapSystem))]
    public partial struct GenerateCellSystem : ISystem
    {
        private NativeParallelHashMap<int3, Entity> _cellMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取 CellMapSystem 的 CellMap
            if (!_cellMap.IsCreated)
            {
                var cellMapSystem = state.World.GetExistingSystemManaged<CellMapSystem>();
                _cellMap = cellMapSystem.CellMap;
            }

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var job = new GenerateCellJob
            {
                CellMap = _cellMap,
                ECB = ecb.AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(ShouldInitializeCell))]
        public partial struct GenerateCellJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeParallelHashMap<int3, Entity> CellMap;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(CellGeneratorAspect generator, [EntityIndexInQuery] int entityIndex)
            {
                var center = generator.Position;
                var range = generator.CoreRange;
                var prefab = generator.CellPrefab;

                for (var x = -range; x <= range; x++)
                for (var y = -range; y <= range; y++)
                for (var z = -range; z <= range; z++)
                {
                    var pos = center + new int3(x, y, z);
                    if (CellMap.ContainsKey(pos)) continue; // 如果 CellMap 中已经存在这个位置的 Cell，则跳过

                    var cell = ECB.Instantiate(entityIndex, prefab);
                    ECB.SetComponent(entityIndex, cell, new LocalTransform
                    {
                        Position = pos,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });

                    // 把 Cell 实体添加到 CellMap 中
                    CellMap.TryAdd(pos, cell);
                }

                ECB.SetComponentEnabled<ShouldInitializeCell>(entityIndex, generator.Self, false);
            }
        }
    }
}