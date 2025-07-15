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
        public NativeHashMap<int3, Entity> CellMap;

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(1024, Allocator.Persistent);
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
        private NativeHashMap<int3, Entity> _cellMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 获取 CellMapSystem 的 CellMap
            var cellMapSystem = state.World.GetExistingSystemManaged<CellMapSystem>();
            if (cellMapSystem == null || !cellMapSystem.CellMap.IsCreated) return;
            _cellMap = cellMapSystem.CellMap;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // 生成 Cell 逻辑
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
                    // 如果 CellMap 中已经存在这个位置的 Cell，则跳过
                    if (_cellMap.ContainsKey(pos)) continue;

                    var cell = ecb.Instantiate(prefab);
                    ecb.SetComponent(cell, new LocalTransform
                    {
                        Position = pos,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });

                    // 把 Cell 实体添加到 CellMap 中
                    _cellMap.TryAdd(pos, cell);
                }

                ecb.SetComponentEnabled<ShouldInitializeCell>(generator.Self, false);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}