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
        public const uint SlowUpdateRateInMS = 1000u;

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

        /// <summary>
        /// 水分扩散系数
        /// </summary>
        public const float MoistureDiffusionCoefficient = 0.15f;

        /// <summary>
        /// 蒸发系数（温度每超过沸点 1 度的蒸发速率）
        /// </summary>
        public const float EvaporationCoefficient = 0.02f;

        /// <summary>
        /// 水的汽化潜热（单位：J）
        /// </summary>
        public const float WaterLatentHeat = 500f;

        /// <summary>
        /// 燃烧基础消耗速率（单位：J/帧）
        /// </summary>
        public const float CombustionBaseRate = 1f;

        /// <summary>
        /// 燃烧温度系数（温度对燃烧速率的影响因子）
        /// </summary>
        public const float CombustionTemperatureFactor = 0.01f;

        /// <summary>
        /// 燃烧热量释放系数（每消耗 1J 能量释放的热量倍数）
        /// </summary>
        public const float CombustionHeatCoefficient = 10f;

        /// <summary>
        /// 爆炸热量释放系数（每消耗 1J 能量释放的热量倍数）
        /// </summary>
        public const float ExplosionHeatCoefficient = 5f;

        /// <summary>
        /// 爆炸冲击系数（每 1J 能量产生的最大冲击力度）
        /// </summary>
        public const float ExplosionImpulseCoefficient = 0.1f;
    }
}