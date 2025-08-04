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
        public const float MaxSpeed = 50f;

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
        public const float DefaultCellScale = 1f;

        /// <summary>
        /// 碰撞时的冲量损失系数
        /// </summary>
        public const float ImpulseLossFactor = 0.8f;

        /// <summary>
        /// 热传导系数
        /// </summary>
        public const float HeatTransferCoefficient = 0.1f;
    }
}