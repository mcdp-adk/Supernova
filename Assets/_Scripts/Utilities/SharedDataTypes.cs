using _Scripts.Components;
using Unity.Mathematics;

namespace _Scripts.Utilities
{
    /// <summary>
    /// 待生成 Cell 数据结构
    /// 用于在生成系统和实例化系统之间传递 Cell 创建信息
    /// </summary>
    public struct PendingCellData
    {
        /// <summary>
        /// Cell 坐标位置
        /// </summary>
        public int3 Coordinate;

        /// <summary>
        /// Cell 类型
        /// </summary>
        public CellTypeEnum CellType;
    }
}
