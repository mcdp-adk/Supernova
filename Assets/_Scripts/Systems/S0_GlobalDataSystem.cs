using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GlobalDataSystem : SystemBase
    {
        // ========== 数据容器 ==========

        public NativeHashMap<int3, Entity> CellMap { get; private set; }
        public NativeQueue<Entity> CellPoolQueue { get; private set; }

        private EntityCommandBuffer _ecb;

        // ========== 生命周期 ==========

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(GlobalConfig.CellMapInitialCapacity, Allocator.Persistent);
            CellPoolQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            if (!InstantiateCell()) return;
            AddCellToQueue();

            var caSlowSystemGroup = World.GetExistingSystemManaged<CaSlowSystemGroup>();
            var caFastSystemGroup = World.GetExistingSystemManaged<CaFastSystemGroup>();
            if (caSlowSystemGroup == null || caFastSystemGroup == null) return;
            Enabled = false;
            caSlowSystemGroup.Enabled = true;
            caFastSystemGroup.Enabled = true;
            Debug.Log("[GlobalDataSystem] 初始化完成，Cellular Automata 系统更新已启用。");
        }

        protected override void OnDestroy()
        {
            // 清理原生容器以避免内存泄漏
            if (CellMap.IsCreated) CellMap.Dispose();
            if (CellPoolQueue.IsCreated) CellPoolQueue.Dispose();
        }

        // ========== 私有方法 ==========

        private bool InstantiateCell()
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

            _ecb = new EntityCommandBuffer(WorldUpdateAllocator);
            for (var i = 0; i < GlobalConfig.MaxCellPoolSize; i++)
                CellUtility.InstantiateFromPrototype(prototype, _ecb);
            _ecb.Playback(EntityManager);

            return true;
        }

        private void AddCellToQueue()
        {
            _ecb = new EntityCommandBuffer(WorldUpdateAllocator);
            foreach (var (_, cell) in SystemAPI.Query<RefRO<CellTag>>().WithAll<CellPendingDequeue>()
                         .WithEntityAccess())
            {
                CellUtility.EnqueueCellIntoPool(cell, _ecb, CellPoolQueue);
            }

            _ecb.Playback(EntityManager);
        }
    }
}