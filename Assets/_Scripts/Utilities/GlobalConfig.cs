using Unity.Mathematics;

namespace _Scripts.Utilities
{
    /// <summary>
    /// 全局配置常量
    /// </summary>
    public static class GlobalConfig
    {
        /// <summary>
        /// Cell Map 初始容量
        /// </summary>
        public const int CellMapInitialCapacity = 4096;

        /// <summary>
        /// Cell 池大小
        /// </summary>
        public const int MaxCellPoolSize = 65536;


        /// <summary>
        /// Cell 默认缩放比例
        /// </summary>
        public static readonly float3 DefaultCellScale = new(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// 更新频率（毫秒），用于配置细胞自动机系统组的固定更新间隔
        /// </summary>
        public const uint UpdateRateInMS = 500u;
    }
}