using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Components
{
    /// <summary>
    /// Cell 类型枚举定义
    /// </summary>
    public enum CellTypeEnum
    {
        Dead = 0,
        Cell1 = -1,
        Cell2 = -2
    }

    // ========== Cell 标识组件 ==========

    /// <summary>
    /// Cell 原型标记 - 用于标识 Cell 预制体
    /// </summary>
    public struct CellPrototypeTag : IComponentData
    {
    }

    /// <summary>
    /// Cell 实例标记 - 用于标识实例化的 Cell
    /// </summary>
    public struct CellTag : IComponentData
    {
    }

    // ========== Cell 状态组件 ==========

    /// <summary>
    /// Cell 类型 - 定义 Cell 的种类
    /// </summary>
    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }

    /// <summary>
    /// Cell 存活状态 - 控制 Cell 是否激活
    /// </summary>
    public struct IsCellAlive : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Cell 坐标 - 存储 Cell 在 3D 空间中的坐标
    /// </summary>
    public struct CellCoordinate : IComponentData
    {
        public int3 Value;
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