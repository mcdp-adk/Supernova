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
        /// 碰撞时的冲量损失系数
        /// </summary>
        public const float ImpulseLossFactor = 0.8f;

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
        public const float DefaultCellScale = 1f;

        #region 物理流动参数
        
        /// <summary>
        /// Powder 侧向移动速度门槛
        /// </summary>
        public const float PowderSideMovementThreshold = 2.0f;
        
        /// <summary>
        /// Liquid 中层扩散速度门槛
        /// </summary>
        public const float LiquidMiddleLayerThreshold = 5.0f;
        
        /// <summary>
        /// Powder 最大尝试位置数
        /// </summary>
        public const int MaxPowderAttempts = 4;
        
        /// <summary>
        /// Liquid 底层最大尝试位置数
        /// </summary>
        public const int MaxLiquidBottomAttempts = 6;
        
        /// <summary>
        /// Liquid 中层最大尝试位置数
        /// </summary>
        public const int MaxLiquidMiddleAttempts = 4;
        
        /// <summary>
        /// 移动到底面中心的速度损耗系数
        /// </summary>
        public const float BottomCenterDamping = 0.95f;
        
        /// <summary>
        /// 移动到底层的速度损耗系数
        /// </summary>
        public const float BottomLayerDamping = 0.85f;
        
        /// <summary>
        /// 移动到中层的速度损耗系数
        /// </summary>
        public const float MiddleLayerDamping = 0.75f;
        
        #endregion
    }
}