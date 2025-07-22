using _Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    /// <summary>
    /// 全局数据管理系统
    /// 负责管理整个应用生命周期内的共享数据结构
    /// 包括 Cell 位置映射和待实例化队列
    /// </summary>
    [UpdateInGroup(typeof(InitializationCellularAutomataSystemGroup), OrderFirst = true)]
    public partial class GlobalDataSystem : SystemBase
    {
        // ========== 全局数据容器 ==========
        
        /// <summary>
        /// Cell 坐标到实体的映射表 - 用于快速查找指定位置的 Cell
        /// </summary>
        public NativeHashMap<int3, Entity> CellMap { get; private set; }

        /// <summary>
        /// 待实例化 Cell 队列 - 存储等待创建的 Cell 数据
        /// </summary>
        public NativeQueue<PendingCellData> PendingCellsToInstantiate { get; private set; }

        // ========== 系统生命周期 ==========
        
        protected override void OnCreate()
        {
            // 初始化全局数据容器
            CellMap = new NativeHashMap<int3, Entity>(
                GlobalConfig.CellMapInitialCapacity, 
                Allocator.Persistent);
            
            PendingCellsToInstantiate = new NativeQueue<PendingCellData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            // 禁用系统更新 - 此系统仅用于数据管理
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            // 清理原生容器以避免内存泄漏
            if (CellMap.IsCreated) 
                CellMap.Dispose();
            
            if (PendingCellsToInstantiate.IsCreated) 
                PendingCellsToInstantiate.Dispose();
        }
    }
}