using _Scripts.Utilities;
using Unity.Entities;

namespace _Scripts.Components
{
    // ========== Cell 标识组件 ==========
    
    public struct CellPrototypeTag : IComponentData
    {
    }
    
    public struct CellTag : IComponentData
    {
    }
    
    // ========== Cell 数据组件 ==========

    /// <summary>
    /// Cell 双缓冲数据存储组件 - 用于存储和切换 Cell 数据
    /// </summary>
    public struct CellDataBuffer : IComponentData
    {
        public CellData Front;
        public CellData Back;
    }

    // ========== Cell 状态组件 ==========
    
    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }
    
    public struct IsCellAlive : IComponentData, IEnableableComponent
    {
    }

    // ========== Cell 缓冲区组件 ==========

    /// <summary>
    /// 待更新 Cell 缓冲区 - 存储待处理的 Cell 状态变更
    /// </summary>
    public struct PendingCellUpdateBuffer : IBufferElementData
    {
        public bool TargetAliveState;
    }
}