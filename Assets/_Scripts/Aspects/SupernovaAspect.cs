using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Aspects
{
    /// <summary>
    /// Supernova Aspect - 封装 Supernova 实体的所有相关操作
    /// 提供 Cell 生成相关的配置和随机生成逻辑
    /// </summary>
    public readonly partial struct SupernovaAspect : IAspect
    {
        public readonly Entity Self;

        // ========== 组件引用 ==========
        private readonly RefRO<LocalTransform> _transform;
        private readonly RefRO<Mass> _mass;
        private readonly RefRO<CellGenerateRange> _generateRange;
        private readonly RefRO<CellGenerateDensity> _generateDensity;
        private readonly DynamicBuffer<CellConfigBuffer> _cellConfigs;

        // ========== 属性接口 ==========
        
        /// <summary>
        /// 位置坐标 - 获取 Supernova 在 3D 空间中的整数坐标
        /// </summary>
        public int3 Position => (int3)_transform.ValueRO.Position;

        /// <summary>
        /// 质量 - 获取 Supernova 的质量值
        /// </summary>
        public int Mass => _mass.ValueRO.Value;

        /// <summary>
        /// 生成范围 - 获取 Cell 生成的最大半径
        /// </summary>
        public int GenerateRange => _generateRange.ValueRO.Value;

        /// <summary>
        /// 生成密度 - 获取 Cell 生成的密度百分比
        /// </summary>
        public float GenerateDensity => _generateDensity.ValueRO.Value;

        // ========== 公共方法 ==========
        
        /// <summary>
        /// 根据权重配置随机获取 Cell 类型
        /// 使用加权随机算法确保不同类型 Cell 按配置比例生成
        /// </summary>
        /// <param name="random">随机数生成器引用</param>
        /// <returns>随机选择的 Cell 类型</returns>
        public CellTypeEnum GetRandomCellType(Random random)
        {
            // 计算总权重
            var totalWeight = CalculateTotalWeight();
            if (totalWeight <= 0)
                return default;

            // 生成随机数并选择对应的 Cell 类型
            var randomValue = random.NextInt(totalWeight);
            return SelectCellTypeByWeight(randomValue);
        }

        // ========== 私有方法 ==========
        
        /// <summary>
        /// 计算所有 Cell 配置的总权重
        /// </summary>
        /// <returns>总权重值</returns>
        private int CalculateTotalWeight()
        {
            var totalWeight = 0;
            for (var i = 0; i < _cellConfigs.Length; ++i)
            {
                totalWeight += _cellConfigs[i].Weight;
            }
            return totalWeight;
        }

        /// <summary>
        /// 根据权重值选择对应的 Cell 类型
        /// </summary>
        /// <param name="randomValue">随机权重值</param>
        /// <returns>选中的 Cell 类型</returns>
        private CellTypeEnum SelectCellTypeByWeight(int randomValue)
        {
            var currentWeight = 0;
            for (var i = 0; i < _cellConfigs.Length; ++i)
            {
                currentWeight += _cellConfigs[i].Weight;
                if (randomValue < currentWeight)
                {
                    return _cellConfigs[i].CellType;
                }
            }

            return default;
        }
    }
}