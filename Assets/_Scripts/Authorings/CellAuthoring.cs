using _Scripts.Components;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    public class CellAuthoring : MonoBehaviour
    {
        [Header("Cell 设置")] [SerializeField] private CellTypeEnum cellType = CellTypeEnum.None;

        private class CellAuthoringBaker : Baker<CellAuthoring>
        {
            public override void Bake(CellAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<CellTag>(entity);
                AddComponent<IsCellAlive>(entity); // 默认设置 Cell 为活跃状态
                AddComponent(entity, new CellType { Value = authoring.cellType });

                AddBuffer<PendingCellUpdateBuffer>(entity);
            }
        }
    }
}