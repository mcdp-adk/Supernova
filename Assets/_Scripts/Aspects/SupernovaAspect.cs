using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Aspects
{
    public readonly partial struct SupernovaAspect : IAspect
    {
        public readonly Entity Self;

        // ========== 组件引用 ==========
        private readonly RefRO<LocalTransform> _transform;
        private readonly RefRO<Mass> _mass;
        private readonly RefRO<ExplosionStrength> _explosionStrength;
        private readonly RefRO<ExplosionAngleClamp> _explosionAngleClamp;
        private readonly RefRO<CellGenerateRange> _generateRange;
        private readonly RefRO<CellGenerateDensity> _generateDensity;
        private readonly DynamicBuffer<CellConfigBuffer> _cellConfigs;
        
        // ========== 属性接口 ==========

        public int3 Coordinate => (int3)_transform.ValueRO.Position;

        public int Mass => _mass.ValueRO.Value;

        public float ExplosionStrength => _explosionStrength.ValueRO.Value;

        public float ExplosionAngleClamp => _explosionAngleClamp.ValueRO.Value;

        public int GenerateRange => _generateRange.ValueRO.Value;

        public float GenerateDensity => _generateDensity.ValueRO.Value;


        // ========== 公共方法 ==========

        public CellTypeEnum GetRandomCellType(Random random)
        {
            // 计算总权重
            var totalWeight = 0;
            for (var i = 0; i < _cellConfigs.Length; ++i)
                totalWeight += _cellConfigs[i].Weight;

            if (totalWeight <= 0)
                return default;

            // 生成随机数并选择对应的 Cell 类型
            var randomValue = random.NextInt(totalWeight);
            var currentWeight = 0;
            for (var i = 0; i < _cellConfigs.Length; ++i)
            {
                currentWeight += _cellConfigs[i].Weight;
                if (randomValue < currentWeight)
                    return _cellConfigs[i].CellType;
            }

            return default;
        }
    }
}