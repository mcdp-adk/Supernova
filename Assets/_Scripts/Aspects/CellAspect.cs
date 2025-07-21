using _Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace _Scripts.Aspects
{
    public readonly partial struct CellAspect : IAspect
    {
        public readonly Entity Self;

        private readonly RefRW<CellType> _cellType;
        private readonly EnabledRefRW<IsCellAlive> _isCellAlive;

        private readonly RefRO<CellCoordinate> _cellCoordinate;
        private readonly RefRW<MaterialMeshInfo> _materialMeshInfo;

        private readonly DynamicBuffer<PendingCellUpdateBuffer> _buffer;

        public CellTypeEnum CellType
        {
            get => _cellType.ValueRO.Value;
            set => SetCellType(value);
        }

        public bool IsAlive
        {
            get => _isCellAlive.ValueRO;
            set => SetCellAliveState(value);
        }

        public int3 Coordinate => _cellCoordinate.ValueRO.Value;

        public DynamicBuffer<PendingCellUpdateBuffer> Buffer => _buffer;

        private void SetCellType(CellTypeEnum targetCellType)
        {
            _cellType.ValueRW.Value = targetCellType;
            _materialMeshInfo.ValueRW = new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                MeshID = new BatchMeshID { value = (uint)targetCellType }
            };
        }

        private void SetCellAliveState(bool targetAliveState)
        {
            _isCellAlive.ValueRW = targetAliveState;
        }
    }
}