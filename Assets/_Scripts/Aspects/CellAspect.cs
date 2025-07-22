using _Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace _Scripts.Aspects
{
    /// <summary>
    /// Cell Aspect - 封装 Cell 实体的所有相关操作
    /// 提供统一的接口来管理 Cell 的状态、类型和渲染
    /// </summary>
    public readonly partial struct CellAspect : IAspect
    {
        public readonly Entity Self;

        // ========== 组件引用 ==========
        private readonly RefRW<CellType> _cellType;
        private readonly EnabledRefRW<IsCellAlive> _isCellAlive;
        private readonly RefRW<LocalTransform> _cellTransform;
        private readonly RefRW<MaterialMeshInfo> _materialMeshInfo;
        private readonly DynamicBuffer<PendingCellUpdateBuffer> _pendingUpdateBuffer;

        // ========== 属性接口 ==========

        /// <summary>
        /// Cell 类型 - 获取或设置 Cell 的类型
        /// </summary>
        public CellTypeEnum CellType
        {
            get => _cellType.ValueRO.Value;
            set => SetCellType(value);
        }

        /// <summary>
        /// 存活状态 - 获取或设置 Cell 是否存活
        /// </summary>
        public bool IsAlive
        {
            get => _isCellAlive.ValueRO;
            set => SetAliveState(value);
        }

        /// <summary>
        /// 坐标位置 - 获取 Cell 在 3D 空间中的坐标
        /// </summary>
        public int3 Coordinate => (int3)_cellTransform.ValueRO.Position;

        /// <summary>
        /// 待更新缓冲区 - 获取待处理的状态更新队列
        /// </summary>
        public DynamicBuffer<PendingCellUpdateBuffer> PendingUpdateBuffer => _pendingUpdateBuffer;

        // ========== 私有方法 ==========

        /// <summary>
        /// 设置 Cell 类型并更新渲染材质
        /// </summary>
        /// <param name="targetCellType">目标 Cell 类型</param>
        private void SetCellType(CellTypeEnum targetCellType)
        {
            _cellType.ValueRW.Value = targetCellType;

            // 同步更新渲染材质和网格
            _materialMeshInfo.ValueRW = new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                MeshID = new BatchMeshID { value = (uint)targetCellType }
            };
        }

        /// <summary>
        /// 设置 Cell 存活状态
        /// </summary>
        /// <param name="targetAliveState">目标存活状态</param>
        private void SetAliveState(bool targetAliveState)
        {
            _isCellAlive.ValueRW = targetAliveState;
        }
    }
}