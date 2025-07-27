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
        public const float DefaultCellScale = 0.5f;

        /// <summary>
        /// 慢系统更新频率（毫秒）
        /// </summary>
        public const uint SlowUpdateRateInMS = 1000u;

        /// <summary>
        /// 快系统更新频率（毫秒）
        /// </summary>
        public const uint FastUpdateRateInMS = 20u;

        /// <summary>
        /// 最大速度限制
        /// </summary>
        public const float MaxVelocity = 100f;

        /// <summary>
        /// 按CellType索引的固定属性配置
        /// </summary>
        public static class CellConfig
        {
            private static readonly CellStateEnum[] _states = { CellStateEnum.Solid, CellStateEnum.Liquid };
            private static readonly int[] _masses = { 1, 2 };
            private static readonly float[] _energies = { 100f, 200f };

            public static CellStateEnum GetState(CellTypeEnum type) => _states[-(int)type - 1];
            public static int GetMass(CellTypeEnum type) => _masses[-(int)type - 1];
            public static float GetEnergy(CellTypeEnum type) => _energies[-(int)type - 1];
        }
    }
}