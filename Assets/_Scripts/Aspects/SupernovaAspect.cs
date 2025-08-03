using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Collections;
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
        private readonly DynamicBuffer<LayerGenerationConfigBuffer> _layerConfigs;
        private readonly DynamicBuffer<LayerCellGenerationConfigBuffer> _cellGenerationConfigs;
        
        // ========== 属性接口 ==========

        public int3 Coordinate => (int3)_transform.ValueRO.Position;

        public int Mass => _mass.ValueRO.Value;
        
        public int MaxRadius
        {
            get
            {
                var maxRadius = 0;
                for (var i = 0; i < _layerConfigs.Length; i++)
                {
                    maxRadius = math.max(maxRadius, _layerConfigs[i].Radius);
                }
                return maxRadius;
            }
        }

        // ========== 公共方法 ==========

        public (int layerIndex, LayerGenerationConfigBuffer layerConfig) GetLayerForDistance(float distance)
        {
            for (var i = 0; i < _layerConfigs.Length; i++)
            {
                if (distance <= _layerConfigs[i].Radius)
                {
                    return (i, _layerConfigs[i]);
                }
            }
            return (-1, default);
        }

        public CellTypeEnum GetRandomCellType(Random random, int layerIndex)
        {
            var totalWeight = 0;
            var layerCellConfigs = new NativeList<LayerCellGenerationConfigBuffer>(Allocator.Temp);
            
            for (var i = 0; i < _cellGenerationConfigs.Length; i++)
            {
                if (_cellGenerationConfigs[i].LayerIndex != layerIndex) continue;
                layerCellConfigs.Add(_cellGenerationConfigs[i]);
                totalWeight += _cellGenerationConfigs[i].Weight;
            }

            if (totalWeight <= 0)
            {
                layerCellConfigs.Dispose();
                return default;
            }

            var randomValue = random.NextInt(totalWeight);
            var currentWeight = 0;
            
            for (var i = 0; i < layerCellConfigs.Length; i++)
            {
                currentWeight += layerCellConfigs[i].Weight;
                if (randomValue >= currentWeight) continue;
                var result = layerCellConfigs[i].CellType;
                layerCellConfigs.Dispose();
                return result;
            }

            layerCellConfigs.Dispose();
            return default;
        }
    }
}