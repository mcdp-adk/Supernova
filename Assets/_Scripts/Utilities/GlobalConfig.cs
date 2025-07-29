namespace _Scripts.Utilities
{
    /// <summary>
    /// 全局配置常量
    /// </summary>
    public static class GlobalConfig
    {
        /// <summary>
        /// 慢系统更新频率（毫秒）
        /// </summary>
        public const uint SlowUpdateRateInMS = 500u;

        /// <summary>
        /// 快系统更新频率（毫秒）
        /// </summary>
        public const uint FastUpdateRateInMS = 20u;

        /// <summary>
        /// 每帧物理步数缩放倍率
        /// </summary>
        public const float PhysicsSpeedScale = 1f;

        /// <summary>
        /// 最大速度限制
        /// </summary>
        public const float MaxSpeed = 5f;

        /// <summary>
        /// 最大 Cell 数量
        /// </summary>
        public const int MaxCellCount = 100000;

        /// <summary>
        /// Cell Map 初始容量
        /// </summary>
        public const int CellMapInitialCapacity = 65536;

        /// <summary>
        /// Cell 池大小
        /// </summary>
        public const int MaxCellPoolSize = 65536;

        /// <summary>
        /// Cell 默认缩放比例
        /// </summary>
        public const float DefaultCellScale = 0.5f;

        /// <summary>
        /// 按 CellType 索引的固定属性配置
        /// </summary>
        public static class CellConfig
        {
            private static readonly CellStateEnum[] States = { CellStateEnum.Solid, CellStateEnum.Liquid };
            private static readonly int[] Masses = { 1, 2 };
            private static readonly float[] Energies = { 100f, 200f };

            public static CellStateEnum GetState(CellTypeEnum type) => States[-(int)type - 1];
            public static int GetMass(CellTypeEnum type) => Masses[-(int)type - 1];
            public static float GetEnergy(CellTypeEnum type) => Energies[-(int)type - 1];
        }
    }
}