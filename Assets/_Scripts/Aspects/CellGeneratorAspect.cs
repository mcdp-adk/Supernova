using _Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Aspects
{
    public readonly partial struct CellGeneratorAspect : IAspect
    {
        public readonly Entity Self;
        private readonly RefRW<LocalTransform> _transform;
        private readonly RefRW<CellGenerateRange> _range;
        private readonly RefRW<CellPrefabs> _prefab;

        public int3 Position
        {
            get => (int3)_transform.ValueRO.Position;
            set => _transform.ValueRW.Position = value;
        }

        public int CoreRange
        {
            get => _range.ValueRO.Value;
            set => _range.ValueRW.Value = value;
        }

        public Entity CellPrefab
        {
            get => _prefab.ValueRO.Value;
            set => _prefab.ValueRW.Value = value;
        }
    }
}