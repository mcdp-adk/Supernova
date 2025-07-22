using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GlobalDataSystem : SystemBase
    {
        // ========== 全局数据容器 ==========
        
        public NativeHashMap<int3, Entity> CellMap { get; private set; }
        public NativeQueue<Entity> CellPoolQueue { get; private set; }
        
        private BeginVariableRateSimulationEntityCommandBufferSystem _ecbSystem;

        // ========== 系统生命周期 ==========

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(GlobalConfig.CellMapInitialCapacity, Allocator.Persistent);
            CellPoolQueue = new NativeQueue<Entity>(Allocator.Persistent);

            // 获取 EndInitializationEntityCommandBufferSystem 以供 OnUpdate 中获取自动执行的 Command Buffer
            _ecbSystem = World.GetOrCreateSystemManaged<BeginVariableRateSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // 获取一个在 InitializationSystemGroup 结束时自动 Playback 的 Command Buffer
            var ecb = _ecbSystem.CreateCommandBuffer();

            var isPreparationComplete = FillCellPoolQueue(ecb);
            if (!isPreparationComplete) return;
            
            var cellularAutomataSystemGroup = World.GetExistingSystemManaged<VariableRateCellularAutomataSystemGroup>();
            if (cellularAutomataSystemGroup == null) return;
            Enabled = false;
            cellularAutomataSystemGroup.Enabled = true;
            Debug.Log("[GlobalDataSystem] VariableRateCellularAutomataSystemGroup 已启用");
        }

        protected override void OnDestroy()
        {
            // 清理原生容器以避免内存泄漏
            if (CellMap.IsCreated) CellMap.Dispose();
            if (CellPoolQueue.IsCreated) CellPoolQueue.Dispose();
        }

        // ========== 私有方法 ==========

        private bool FillCellPoolQueue(EntityCommandBuffer ecb)
        {
            Entity prototype;

            // 获取 Cell 原型实体（如果报错则跳过更新）
            try
            {
                prototype = SystemAPI.GetSingletonEntity<CellPrototypeTag>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GlobalDataSystem] 获取 CellPrototypeTag 失败: {ex.Message}");
                return false; // 没有原型实体，跳过本帧更新
            }

            var instantiateJob = new InstantiateCellJob
            {
                ECB = ecb,
                CellPoolQueue = CellPoolQueue,
                Prototype = prototype
            };

            instantiateJob.Schedule().Complete();

            return true;
        }

        // ========== 作业定义 ==========

        [BurstCompile]
        private struct InstantiateCellJob : IJob
        {
            public EntityCommandBuffer ECB;
            public NativeQueue<Entity> CellPoolQueue;
            [ReadOnly] public Entity Prototype;

            public void Execute()
            {
                for (var i = 0; i < GlobalConfig.MaxCellPoolSize; i++)
                    CellPoolQueue.Enqueue(CellUtility.InitiateFromPrototype(Prototype, ECB));
            }
        }
    }
}