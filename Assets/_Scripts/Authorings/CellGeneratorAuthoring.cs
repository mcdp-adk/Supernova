using _Scripts.Components;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    public class CellGeneratorAuthoring : MonoBehaviour
    {
        [Header("初始 Cell 生成设置")] [Tooltip("从中心到生成边缘的距离")] [SerializeField]
        private int generateRange;

        [Header("Cell Prefabs")] [SerializeField]
        private GameObject cellPrefab;

        private class CenterAuthoringBaker : Baker<CellGeneratorAuthoring>
        {
            public override void Bake(CellGeneratorAuthoring authoring)
            {
                // 确保位置四舍五入为最接近的整数
                authoring.transform.position = new Vector3(
                    Mathf.RoundToInt(authoring.transform.position.x),
                    Mathf.RoundToInt(authoring.transform.position.y),
                    Mathf.RoundToInt(authoring.transform.position.z)
                );

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<CellGeneratorTag>(entity);

                AddComponent<ShouldInitializeCell>(entity);

                AddComponent(entity, new CellGeneratorData
                {
                    CoreRange = authoring.generateRange,
                    CellPrefab = GetEntity(authoring.cellPrefab, TransformUsageFlags.Renderable)
                });
            }
        }

        private void OnDrawGizmos()
        {
            // 绘制生成
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position,
                new Vector3(generateRange * 2 + 1f, generateRange * 2 + 1f, generateRange * 2 + 1f));
        }
    }
}