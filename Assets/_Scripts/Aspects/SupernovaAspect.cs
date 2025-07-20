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

        public int3 Position => (int3)_transform.ValueRO.Position;

        public int Mass => _mass.ValueRO.Value;

        public int GenerateRange => _range.ValueRO.Value;
        public float GenerateDensity => _density.ValueRO.Value;
    }
}