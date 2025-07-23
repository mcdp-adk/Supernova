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
            // 生成 Cell
            var isInstantiateComplete = InstantiateCell();
            if (!isInstantiateComplete) return;

            // 添加 Cell 到队列
            AddCellToQueue();

            // 获取 VariableRateCellularAutomataSystemGroup 系统并启用更新
            var cellularAutomataSystemGroup = World.GetExistingSystemManaged<VariableRateCellularAutomataSystemGroup>();
            if (cellularAutomataSystemGroup == null) return;
            Enabled = false;
            cellularAutomataSystemGroup.Enabled = true;
            Debug.Log("初始化成功！已启用更新：[GlobalDataSystem] VariableRateCellularAutomataSystemGroup");
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
            foreach (var (_, cell) in SystemAPI.Query<RefRO<CellTag>>().WithAll<CellPendingDequeue>().WithEntityAccess())
            {
                CellUtility.EnqueueCellIntoPool(cell, _ecb, CellPoolQueue);
            }

            _ecb.Playback(EntityManager);
        }
    }
}