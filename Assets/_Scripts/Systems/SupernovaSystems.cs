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
    public partial struct CellGenerationSystem : ISystem
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

            var job = new BatchCellGenerationJob
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
        public partial struct BatchCellGenerationJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public NativeParallelHashMap<int3, Entity> CellMap;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(SupernovaAspect generator, [EntityIndexInQuery] int entityIndex)
            {
                var center = generator.Position;
                var range = generator.GenerateRange;
                var density = generator.GenerateDensity;
                var prefabs = generator.Prefabs;

                // 计算总权重
                var totalWeight = 0;
                for (var i = 0; i < prefabs.Length; i++) totalWeight += prefabs[i].Weight;

                // 获取随机种子，与生成中心的位置相关
                var random = new Random(math.hash(center));

                for (var x = -range; x <= range; ++x)
                for (var y = -range; y <= range; ++y)
                for (var z = -range; z <= range; ++z)
                {
                    var pos = center + new int3(x, y, z);

                    // 根据密度决定是否生成 Cell
                    if (random.NextInt(0, 100) >= density) continue;
                    // 如果 CellMap 中已经存在这个位置的 Cell，则跳过
                    if (CellMap.ContainsKey(pos)) continue;

                    // 根据 Cell 权重随机选择 Prefab
                    var pick = random.NextInt(0, totalWeight);
                    var acc = 0;
                    var chosenPrefab = Entity.Null;
                    for (var i = 0; i < prefabs.Length; i++)
                    {
                        acc += prefabs[i].Weight;
                        if (pick >= acc) continue;
                        chosenPrefab = prefabs[i].Prefab;
                        break;
                    }

                    // 如果没有选择到 Prefab，则跳过
                    if (chosenPrefab == Entity.Null) continue;

                    // 实例化 Cell 实体
                    var cell = ECB.Instantiate(entityIndex, chosenPrefab);
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