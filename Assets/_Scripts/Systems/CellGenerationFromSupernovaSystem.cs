using _Scripts.Aspects;
using _Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
    public partial struct CellGenerationFromSupernovaSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取 GlobalData
            if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
                _pendingCells = globalDataSystem.PendingCellsToInstantiate;
            }

            // 收集需要实例化的 Cell 位置和类型
            var collectJob = new CollectCellPositionsToGenerateJob
            {
                PendingCells = _pendingCells.AsParallelWriter()
            };

            state.Dependency = collectJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(ShouldInitializeCell))]
        private partial struct CollectCellPositionsToGenerateJob : IJobEntity
        {
            public NativeQueue<PendingCellData>.ParallelWriter PendingCells;

            private void Execute(SupernovaAspect supernova, EnabledRefRW<ShouldInitializeCell> shouldInitializeCell)
            {
                var random = new Random(math.hash(supernova.Position));

                // 遍历并判断是否需要生成 Cell
                for (var x = -supernova.GenerateRange; x <= supernova.GenerateRange; ++x)
                for (var y = -supernova.GenerateRange; y <= supernova.GenerateRange; ++y)
                for (var z = -supernova.GenerateRange; z <= supernova.GenerateRange; ++z)
                {
                    // 获取 Cell 在 World 中的位置
                    var offset = new int3(x, y, z);
                    var generatePosition = supernova.Position + offset;

                    if (random.NextFloat(0, 100f) >= supernova.GenerateDensity) continue; // 根据密度决定是否生成 Cell
                    if (math.length(offset) >= supernova.GenerateRange) continue; // 只保留球体内的点

                    // 根据权重随机选择 Cell 类型
                    var cellType = supernova.GetRandomCellType(ref random);

                    // 加入生成队列
                    PendingCells.Enqueue(new PendingCellData
                    {
                        Position = generatePosition,
                        CellType = cellType
                    });
                }

                // 生成完毕后禁用 ShouldInitializeCell 组件
                shouldInitializeCell.ValueRW = false;
            }
        }
    }
}