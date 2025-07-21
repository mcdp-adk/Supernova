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
                var generateRangeSq = supernova.GenerateRange * supernova.GenerateRange;

                // 按距离层遍历，从球心向外生成
                for (var distance = 0; distance <= supernova.GenerateRange; distance++)
                {
                    var distanceSq = distance * distance;
                    var nextDistanceSq = (distance + 1) * (distance + 1);

                    // 遍历可能包含当前距离层的最小立方体
                    for (var x = -distance; x <= distance; ++x)
                    for (var y = -distance; y <= distance; ++y)
                    for (var z = -distance; z <= distance; ++z)
                    {
                        var offset = new int3(x, y, z);
                        var currentDistanceSq = math.lengthsq(offset);

                        // 只处理距离在当前层范围内的点
                        if (currentDistanceSq < distanceSq || currentDistanceSq >= nextDistanceSq) continue;

                        // 确保在球体范围内
                        if (currentDistanceSq >= generateRangeSq) continue;

                        // 根据密度决定是否生成 Cell
                        if (random.NextFloat(0, 100f) >= supernova.GenerateDensity) continue;

                        // 获取生成位置和 Cell 类型
                        var generatePosition = supernova.Position + offset;
                        var cellType = supernova.GetRandomCellType(ref random);

                        // 加入生成队列
                        PendingCells.Enqueue(new PendingCellData
                        {
                            Coordinate = generatePosition,
                            CellType = cellType
                        });
                    }
                }

                // 生成完毕后禁用 ShouldInitializeCell 组件
                shouldInitializeCell.ValueRW = false;
            }
        }
    }
}