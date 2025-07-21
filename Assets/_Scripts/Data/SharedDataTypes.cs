using _Scripts.Components;
using Unity.Mathematics;

namespace _Scripts.Data
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

    /// <summary>
    /// 全局配置常量
    /// </summary>
    public static class GlobalConfig
    {
        /// <summary>
        /// 每帧最大 Cell 实例化数量 - 避免单帧生成过多导致性能问题
        /// </summary>
        public const int MaxCellsPerFrame = 512;

        /// <summary>
        /// Cell 默认缩放比例
        /// </summary>
        public static readonly float3 DefaultCellScale = new float3(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// Cell Map 初始容量
        /// </summary>
        public const int CellMapInitialCapacity = 4096;
    }
}
