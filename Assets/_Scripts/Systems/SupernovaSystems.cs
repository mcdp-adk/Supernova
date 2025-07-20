using _Scripts.Aspects;
using _Scripts.Components;
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
    #region Custom System Groups

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializationCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    #endregion

    #region GlobalDataSystem

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup), OrderFirst = true)]
    public partial class GlobalDataSystem : SystemBase
    {
        public NativeHashMap<int3, Entity> CellMap;
        public NativeQueue<PendingCellData> PendingCellsToInstantiate;

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(4096, Allocator.Persistent);
            PendingCellsToInstantiate = new NativeQueue<PendingCellData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (CellMap.IsCreated) CellMap.Dispose();
            if (PendingCellsToInstantiate.IsCreated) PendingCellsToInstantiate.Dispose();
        }
    }

    #endregion

    #region InitializationCellularAutomataSystemGroup

    public struct PendingCellData
    {
        public int3 Position;
        public CellTypeEnum CellType;
    }

    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup))]
    public partial struct CellInstantiationSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeQueue<PendingCellData> _pendingCells;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellPrototypeTag>();
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

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var prototype = SystemAPI.GetSingletonEntity<CellPrototypeTag>();

            // 持续实例化 Cell
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

        [BurstCompile]
        private struct ContinuousInstantiateCellsJob : IJob
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<int3, Entity> CellMap;
            public NativeQueue<PendingCellData> PendingCells;
            [ReadOnly] public Entity Prototype;

            public void Execute()
            {
                const int maxCellsPerFrame = 512;
                var processedCount = 0;

                while (PendingCells.TryDequeue(out var pendingCellData) && processedCount < maxCellsPerFrame)
                {
                    // 检查位置是否已被占用
                    if (CellMap.ContainsKey(pendingCellData.Position)) continue;

                    // 从 Prototype 复制并实例化 Cell
                    var cell = ECB.Instantiate(Prototype);

                    ECB.SetName(cell,
                        $"Cell_{pendingCellData.Position.x}_{pendingCellData.Position.y}_{pendingCellData.Position.z}");
                    ECB.RemoveComponent<CellPrototypeTag>(cell);
                    ECB.SetComponent(cell,
                        new LocalToWorld
                        {
                            Value = float4x4.TRS(pendingCellData.Position,
                                quaternion.identity,
                                new float3(0.5f, 0.5f, 0.5f))
                        });

                    // 设置 Cell 类型以及显示的 Mesh 和 Material
                    ECB.SetComponent(cell, new CellType { Value = pendingCellData.CellType });
                    ECB.SetComponent(cell, new MaterialMeshInfo
                    {
                        MaterialID = new BatchMaterialID { value = (uint)pendingCellData.CellType },
                        MeshID = new BatchMeshID { value = (uint)pendingCellData.CellType }
                    });

                    // 把 Cell 实体添加到 CellMap 中
                    CellMap.TryAdd(pendingCellData.Position, cell);
                    processedCount++;
                }
            }
        }
    }

    #endregion

    #region VariableRateCellularAutomataSystemGroup

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

            private void Execute(SupernovaAspect supernova)
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
            }
        }
    }

    #endregion
}