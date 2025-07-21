using _Scripts.Components;
using _Scripts.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace _Scripts.Systems
{
    /// <summary>
    /// Cell 实例化系统 - 负责将待生成的 Cell 数据转换为实际的 Entity
    /// 采用分帧实例化策略避免单帧生成过多 Entity 导致性能问题
    /// </summary>
    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup))]
    public partial struct CellInstantiationSystem : ISystem
    {
        // ========== 全局数据引用 ==========
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

        // ========== 系统生命周期 ==========

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellPrototypeTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据容器引用
            InitializeGlobalDataReferences(ref state);

            // 获取 Cell 原型实体
            var prototype = SystemAPI.GetSingletonEntity<CellPrototypeTag>();
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // 执行连续实例化作业
            var instantiateJob = new ContinuousInstantiateCellsJob
            {
                ECB = ecb,
                CellMap = _cellMap,
                PendingCells = _pendingCells,
                Prototype = prototype
            };

            state.Dependency = instantiateJob.Schedule(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
        }

        // ========== 私有方法 ==========

        /// <summary>
        /// 初始化全局数据容器引用
        /// </summary>
        private void InitializeGlobalDataReferences(ref SystemState state)
        {
            if (_cellMap.IsCreated && _pendingCells.IsCreated) return;
            var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
            _cellMap = globalDataSystem.CellMap;
            _pendingCells = globalDataSystem.PendingCellsToInstantiate;
        }

        // ========== 作业定义 ==========

        /// <summary>
        /// 连续实例化 Cell 作业
        /// 每帧处理固定数量的待生成 Cell，确保性能稳定
        /// </summary>
        [BurstCompile]
        private struct ContinuousInstantiateCellsJob : IJob
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<PendingCellData> PendingCells;
            [ReadOnly] public Entity Prototype;

            public void Execute()
            {
                var processedCount = 0;

                // 分帧处理待生成的 Cell
                while (PendingCells.TryDequeue(out var pendingCellData) &&
                       processedCount < GlobalConfig.MaxCellsPerFrame)
                {
                    // 检查位置冲突
                    if (CellMap.ContainsKey(pendingCellData.Coordinate))
                        continue;

                    // 实例化并配置 Cell
                    var cell = CreateCellFromPrototype(pendingCellData);

                    // 注册到全局映射表
                    CellMap.TryAdd(pendingCellData.Coordinate, cell);
                    processedCount++;
                }
            }

            /// <summary>
            /// 从原型创建 Cell 实体并设置相关组件
            /// </summary>
            private Entity CreateCellFromPrototype(PendingCellData pendingCellData)
            {
                var cell = ECB.Instantiate(Prototype);

                // 清理原型标记并设置基础组件
                ConfigureBasicCellComponents(cell);

                // 设置位置和渲染相关组件
                ConfigureCellTransformAndRendering(cell, pendingCellData);

                // 设置 Cell 类型和材质
                ConfigureCellTypeAndMaterial(cell, pendingCellData.CellType);

                return cell;
            }

            /// <summary>
            /// 配置 Cell 的基础组件
            /// </summary>
            private void ConfigureBasicCellComponents(Entity cell)
            {
                ECB.SetName(cell, "");
                ECB.RemoveComponent<CellPrototypeTag>(cell);
                ECB.SetComponentEnabled<IsCellAlive>(cell, true);
            }

            /// <summary>
            /// 配置 Cell 的变换和渲染组件
            /// </summary>
            private void ConfigureCellTransformAndRendering(Entity cell, PendingCellData pendingCellData)
            {
                // 设置坐标组件
                ECB.SetComponent(cell, new CellCoordinate { Value = pendingCellData.Coordinate });

                // 设置世界变换矩阵用于渲染
                ECB.SetComponent(cell, new LocalToWorld
                {
                    Value = float4x4.TRS(
                        pendingCellData.Coordinate,
                        quaternion.identity,
                        GlobalConfig.DefaultCellScale)
                });
            }

            /// <summary>
            /// 配置 Cell 的类型和材质组件
            /// </summary>
            private void ConfigureCellTypeAndMaterial(Entity cell, CellTypeEnum cellType)
            {
                // 设置 Cell 类型
                ECB.SetComponent(cell, new CellType { Value = cellType });

                // 设置渲染材质和网格
                ECB.SetComponent(cell, new MaterialMeshInfo
                {
                    MaterialID = new BatchMaterialID { value = (uint)cellType },
                    MeshID = new BatchMeshID { value = (uint)cellType }
                });
            }
        }
    }
}