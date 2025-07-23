using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace _Scripts.Aspects
{
    public readonly partial struct CellAspect : IAspect
    {
        public readonly Entity Self;

        // ========== 组件引用 ==========
        private readonly EnabledRefRW<IsCellAlive> _isCellAlive;
        private readonly RefRW<CellType> _cellType;
        private readonly RefRW<LocalTransform> _cellTransform;
        private readonly RefRW<MaterialMeshInfo> _materialMeshInfo;
        private readonly DynamicBuffer<PendingCellUpdateBuffer> _pendingUpdateBuffer;

        // ========== 属性接口 ==========

        public CellTypeEnum CellType
        {
            get => _cellType.ValueRO.Value;
            set => SetCellType(value);
        }

        public int3 Coordinate => (int3)_cellTransform.ValueRO.Position;

        public DynamicBuffer<PendingCellUpdateBuffer> PendingUpdateBuffer => _pendingUpdateBuffer;

        // ========== 私有方法 ==========

        private void SetCellType(CellTypeEnum targetCellType)
        {
            _isCellAlive.ValueRW = targetCellType != CellTypeEnum.None;
            _cellType.ValueRW.Value = targetCellType;
            _materialMeshInfo.ValueRW = new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                MeshID = new BatchMeshID { value = (uint)targetCellType }
            };
        }
    }
}