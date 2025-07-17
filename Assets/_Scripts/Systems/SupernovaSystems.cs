using _Scripts.Aspects;
using _Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    #region System Groups and Structs

    public struct PendingCellData
    {
        public int3 Position;
        public Entity Prefab;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializationCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    #endregion

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup), OrderFirst = true)]
    public partial class GlobalDataSystem : SystemBase
    {
        public NativeParallelHashMap<int3, Entity> CellMap;
        public NativeQueue<PendingCellData> PendingCells;

        protected override void OnCreate()
        {
            CellMap = new NativeParallelHashMap<int3, Entity>(1024, Allocator.Persistent);
            PendingCells = new NativeQueue<PendingCellData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (CellMap.IsCreated) CellMap.Dispose();
            if (PendingCells.IsCreated) PendingCells.Dispose();
        }
    }

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup))]
    public partial struct CellInstantiationSystem : ISystem
    {
        private NativeParallelHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局 CellMap 和 PendingCells
            if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
                _pendingCells = globalDataSystem.PendingCells;
            }

            // 如果没有待处理的Cell，跳过
            if (_pendingCells.Count == 0) return;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var instantiateJob = new ContinuousInstantiateCellsJob
            {
                PendingCells = _pendingCells,
                CellMap = _cellMap,
                ECB = ecb
            };

            state.Dependency = instantiateJob.Schedule(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        private struct ContinuousInstantiateCellsJob : IJob
        {
            public NativeQueue<PendingCellData> PendingCells;
            [NativeDisableParallelForRestriction] public NativeParallelHashMap<int3, Entity> CellMap;
            public EntityCommandBuffer ECB;

            public void Execute()
            {
                // 自定义每帧处理的 Cell 数量
                const int maxCellsPerFrame = 1024;
                var processedCount = 0;

                while (PendingCells.TryDequeue(out var cellData) && processedCount < maxCellsPerFrame)
                {
                    // 再次检查位置是否已被占用
                    if (CellMap.ContainsKey(cellData.Position))
                    {
                        processedCount++;
                        continue;
                    }

                    // 实例化 Cell 实体
                    var cell = ECB.Instantiate(cellData.Prefab);
                    ECB.SetComponent(cell, new LocalTransform
                    {
                        Position = cellData.Position,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });

                    // 把 Cell 实体添加到 CellMap 中
                    CellMap.TryAdd(cellData.Position, cell);
                    processedCount++;
                }
            }
        }
    }

    [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
    public partial struct CellGenerationSystem : ISystem
    {
        private NativeParallelHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局 CellMap 和 PendingCells
            if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
                _pendingCells = globalDataSystem.PendingCells;
            }

            // 收集需要实例化的 Cell 位置
            var collectJob = new CollectCellPositionsJob
            {
                CellMap = _cellMap,
                PendingCells = _pendingCells.AsParallelWriter()
            };

            state.Dependency = collectJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            // 因为收集工作完成，禁用 ShouldInitializeCell 组件
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<ShouldInitializeCell>>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<ShouldInitializeCell>(entity, false);
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(ShouldInitializeCell))]
        private partial struct CollectCellPositionsJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int3, Entity> CellMap;
            public NativeQueue<PendingCellData>.ParallelWriter PendingCells;

            private void Execute(SupernovaAspect generator)
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

                    // 添加到待实例化队列
                    PendingCells.Enqueue(new PendingCellData
                    {
                        Position = pos,
                        Prefab = chosenPrefab
                    });
                }
            }
        }
    }
}