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
    /// <summary>
    /// 全局数据管理系统
    /// 负责管理整个应用生命周期内的共享数据结构
    /// 包括 Cell 位置映射和待实例化队列
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GlobalDataSystem : SystemBase
    {
        // ========== 全局数据容器 ==========

        /// <summary>
        /// Cell 坐标到实体的映射表 - 用于快速查找指定位置的 Cell
        /// </summary>
        public NativeHashMap<int3, Entity> CellMap { get; private set; }

        /// <summary>
        /// Cell 池队列 - 用于存储可复用的 Cell 实体
        /// </summary>
        public NativeQueue<Entity> CellPoolQueue { get; private set; }

        /// <summary>
        /// 待实例化 Cell 队列 - 存储等待创建的 Cell 数据
        /// </summary>
        public NativeQueue<PendingCellData> PendingCellsToInstantiate { get; private set; }

        private EndInitializationEntityCommandBufferSystem _ecbSystem;

        // ========== 系统生命周期 ==========

        protected override void OnCreate()
        {
            // 初始化全局数据容器
            CellMap = new NativeHashMap<int3, Entity>(GlobalConfig.CellMapInitialCapacity, Allocator.Persistent);
            CellPoolQueue = new NativeQueue<Entity>(Allocator.Persistent);
            PendingCellsToInstantiate = new NativeQueue<PendingCellData>(Allocator.Persistent);

            // 获取 EndInitializationEntityCommandBufferSystem 以供 OnUpdate 中获取自动执行的 Command Buffer
            _ecbSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // 获取一个在 InitializationSystemGroup 结束时自动 Playback 的 Command Buffer
            var ecb = _ecbSystem.CreateCommandBuffer();

            FillCellPoolQueue(ecb);
        }

        protected override void OnDestroy()
        {
            // 清理原生容器以避免内存泄漏
            if (CellMap.IsCreated) CellMap.Dispose();
            if (CellPoolQueue.IsCreated) CellPoolQueue.Dispose();
            if (PendingCellsToInstantiate.IsCreated) PendingCellsToInstantiate.Dispose();
        }

        // ========== 私有方法 ==========

        private void FillCellPoolQueue(EntityCommandBuffer ecb)
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
                return; // 没有原型实体，跳过本帧更新
            }

            var instantiateJob = new InstantiateCellJob
            {
                ECB = ecb,
                CellPoolQueue = CellPoolQueue,
                Prototype = prototype
            };

            instantiateJob.Schedule().Complete();
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
                var currentCount = CellPoolQueue.Count;
                var maxToCreate = math.min(
                    GlobalConfig.MaxCellsPerFrame,
                    GlobalConfig.MaxCellPoolSize - currentCount
                );

                for (var i = 0; i < maxToCreate; i++)
                    CellPoolQueue.Enqueue(CellUtility.InitiateFromPrototype(Prototype, ECB));
            }
        }
    }
}