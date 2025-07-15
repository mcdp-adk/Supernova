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
        private readonly RefRW<CellGeneratorData> _data;

        public int3 Position
        {
            get => (int3)_transform.ValueRO.Position;
            set => _transform.ValueRW.Position = value;
        }

        public int CoreRange
        {
            get => _data.ValueRO.CoreRange;
            set => _data.ValueRW.CoreRange = value;
        }

        public Entity CellPrefab
        {
            get => _data.ValueRO.CellPrefab;
            set => _data.ValueRW.CellPrefab = value;
        }
    }
}