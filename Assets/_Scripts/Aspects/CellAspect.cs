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

        // 状态标识
        private readonly EnabledRefRW<IsCellAlive> _isCellAlive;

        // 基本属性
        private readonly RefRW<CellType> _cellType;

        // 物理属性
        private readonly RefRW<Velocity> _velocity;
        private readonly RefRW<Temperature> _temperature;

        // 化学属性
        private readonly RefRW<Energy> _energy;

        // Buffer
        private readonly DynamicBuffer<PendingChangeBuffer> _pendingChangeBuffer;

        // 其他
        private readonly RefRW<LocalTransform> _cellTransform;
        private readonly RefRW<MaterialMeshInfo> _materialMeshInfo;


        // ========== 属性接口 ==========

        // 暴露只读数据
        public int3 Coordinate => (int3)_cellTransform.ValueRO.Position;

        // 暴露读写数据
        public CellTypeEnum CellType
        {
            get => _cellType.ValueRO.Value;
            set => SetCellType(value);
        }
        
        public float3 Velocity
        {
            get => _velocity.ValueRO.Value;
            set => _velocity.ValueRW.Value = value;
        }
        
        public float Temperature
        {
            get => _temperature.ValueRO.Value;
            set => _temperature.ValueRW.Value = value;
        }
        
        public float Energy
        {
            get => _energy.ValueRO.Value;
            set => _energy.ValueRW.Value = value;
        }

        // 暴露组件引用
        public RefRW<LocalTransform> LocalTransform => _cellTransform;
        public DynamicBuffer<PendingChangeBuffer> PendingChangeBuffer => _pendingChangeBuffer;


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