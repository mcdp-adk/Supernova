using _Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Aspects
{
    public readonly partial struct CellAspect : IAspect
    {
        public readonly Entity Self;
        private readonly RefRO<LocalTransform> _transform;

        private readonly EnabledRefRW<IsCellAlive> _isCellAlive;
        private readonly RefRW<CellType> _cellType;
        private readonly DynamicBuffer<PendingCellUpdateBuffer> _buffer;

        public int3 Position => (int3)_transform.ValueRO.Position;

        public bool IsAlive
        {
            get => _isCellAlive.ValueRO;
            set => _isCellAlive.ValueRW = value;
        }

        public CellTypeEnum CellType
        {
            get => _cellType.ValueRO.Value;
            set => _cellType.ValueRW.Value = value;
        }

        public DynamicBuffer<PendingCellUpdateBuffer> Buffer => _buffer;
    }
}