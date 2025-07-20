using _Scripts.Components;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    public class SupernovaAuthoring : MonoBehaviour
    {
        [Header("超新星设置")] [SerializeField] private int mass = 100;

        [Header("Cell 生成设置")] [Tooltip("从中心到生成边缘的距离")] [SerializeField]
        private int generateRange;

        [Range(0, 20)] [SerializeField] private float generateDensity = 10f;

        private class CenterAuthoringBaker : Baker<SupernovaAuthoring>
        {
            public override void Bake(SupernovaAuthoring authoring)
            {
                // 依赖于 Transform，当 Transform 发生变化时触发 Bake
                DependsOn(authoring.transform);

                // 确保位置四舍五入为最接近的整数
                authoring.transform.position = new Vector3(
                    Mathf.RoundToInt(authoring.transform.position.x),
                    Mathf.RoundToInt(authoring.transform.position.y),
                    Mathf.RoundToInt(authoring.transform.position.z)
                );

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<CellGeneratorTag>(entity);
                AddComponent<ShouldInitializeCell>(entity); // 默认需要初始化 Cell

                AddComponent(entity, new Mass { Value = authoring.mass });

                AddComponent(entity, new CellGenerateRange { Value = authoring.generateRange });
                AddComponent(entity, new CellGenerateDensity { Value = authoring.generateDensity });
            }
        }

        private void OnDrawGizmos()
        {
            // 绘制生成
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, generateRange);
        }
    }
}