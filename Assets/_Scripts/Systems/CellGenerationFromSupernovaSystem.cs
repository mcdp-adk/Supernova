using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    /// <summary>
    /// Cell 生成系统 - 从 Supernova 生成 Cell
    /// 负责根据 Supernova 的配置参数计算并收集需要生成的 Cell 位置和类型
    /// </summary>
    [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
    public partial struct CellGenerationFromSupernovaSystem : ISystem
    {
        // ========== 全局数据引用 ==========
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

        // ========== 系统生命周期 ==========

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShouldInitializeCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据容器引用
            InitializeGlobalDataReferences(ref state);

            // 执行 Cell 位置收集作业
            var collectJob = new CollectCellPositionsJob
            {
                PendingCells = _pendingCells.AsParallelWriter()
            };

            state.Dependency = collectJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        // ========== 私有方法 ==========

        /// <summary>
        /// 初始化全局数据容器引用
        /// </summary>
        private void InitializeGlobalDataReferences(ref SystemState state)
        {
            if (!_cellMap.IsCreated || !_pendingCells.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
                _pendingCells = globalDataSystem.PendingCellsToInstantiate;
            }
        }

        // ========== 作业定义 ==========

        /// <summary>
        /// 收集 Cell 生成位置作业
        /// 根据 Supernova 的参数按球体分层生成 Cell 坐标
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(ShouldInitializeCell))]
        private partial struct CollectCellPositionsJob : IJobEntity
        {
            public NativeQueue<PendingCellData>.ParallelWriter PendingCells;

            private void Execute(SupernovaAspect supernova, EnabledRefRW<ShouldInitializeCell> shouldInitializeCell)
            {
                var random = new Random(math.hash(supernova.Position));

                // 按距离层生成 Cell - 从中心向外扩散
                GenerateCellsByDistanceLayers(supernova, ref random);

                // 生成完毕后禁用初始化标记
                shouldInitializeCell.ValueRW = false;
            }

            /// <summary>
            /// 按距离层生成 Cell
            /// 采用分层算法确保 Cell 从球心向外均匀分布
            /// </summary>
            private void GenerateCellsByDistanceLayers(SupernovaAspect supernova, ref Random random)
            {
                var generateRangeSq = supernova.GenerateRange * supernova.GenerateRange;

                for (var distance = 0; distance <= supernova.GenerateRange; distance++)
                {
                    var distanceSq = distance * distance;
                    var nextDistanceSq = (distance + 1) * (distance + 1);

                    // 遍历当前距离层的所有可能位置
                    ProcessDistanceLayer(supernova, ref random, distance, distanceSq, nextDistanceSq, generateRangeSq);
                }
            }

            /// <summary>
            /// 处理指定距离层的 Cell 生成
            /// </summary>
            private void ProcessDistanceLayer(SupernovaAspect supernova, ref Random random,
                int distance, int distanceSq, int nextDistanceSq, int generateRangeSq)
            {
                for (var x = -distance; x <= distance; ++x)
                for (var y = -distance; y <= distance; ++y)
                for (var z = -distance; z <= distance; ++z)
                {
                    var offset = new int3(x, y, z);
                    var currentDistanceSq = math.lengthsq(offset);

                    // 检查是否在当前距离层范围内
                    if (!IsInCurrentDistanceLayer(currentDistanceSq, distanceSq, nextDistanceSq, generateRangeSq))
                        continue;

                    // 根据密度决定是否生成
                    if (!ShouldGenerateCell(supernova.GenerateDensity, ref random))
                        continue;

                    // 创建 Cell 生成数据
                    var generatePosition = supernova.Position + offset;
                    var cellType = supernova.GetRandomCellType(ref random);

                    PendingCells.Enqueue(new PendingCellData
                    {
                        Coordinate = generatePosition,
                        CellType = cellType
                    });
                }
            }

            /// <summary>
            /// 检查位置是否在当前距离层范围内
            /// </summary>
            private static bool IsInCurrentDistanceLayer(float currentDistanceSq, float distanceSq,
                float nextDistanceSq, float generateRangeSq)
            {
                return currentDistanceSq >= distanceSq &&
                       currentDistanceSq < nextDistanceSq &&
                       currentDistanceSq < generateRangeSq;
            }

            /// <summary>
            /// 根据密度参数决定是否应该生成 Cell
            /// </summary>
            private static bool ShouldGenerateCell(float density, ref Random random)
            {
                return random.NextFloat(0, 100f) < density;
            }
        }
    }
}