using _Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Aspects
{
    public readonly partial struct SupernovaAspect : IAspect
    {
        public readonly Entity Self;
        private readonly RefRO<LocalTransform> _transform;

        private readonly RefRO<Mass> _mass;

        private readonly RefRO<CellGenerateRange> _range;
        private readonly RefRO<CellGenerateDensity> _density;
        private readonly DynamicBuffer<CellConfigBuffer> _cellConfigs;

        public int3 Position => (int3)_transform.ValueRO.Position;

        public int Mass => _mass.ValueRO.Value;

        public int GenerateRange => _range.ValueRO.Value;
        public float GenerateDensity => _density.ValueRO.Value;

        public CellTypeEnum GetRandomCellType(ref Random random)
        {
            // 计算总权重
            var totalWeight = 0;
            for (var i = 0; i < _cellConfigs.Length; ++i) totalWeight += _cellConfigs[i].Weight;

            // 生成随机数
            var randomValue = random.NextInt(totalWeight);

            // 根据权重选择
            var currentWeight = 0;
            for (var i = 0; i < _cellConfigs.Length; ++i)
            {
                currentWeight += _cellConfigs[i].Weight;
                if (randomValue < currentWeight) return _cellConfigs[i].CellType;
            }

            return default;
        }
    }
}