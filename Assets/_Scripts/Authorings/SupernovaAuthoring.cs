using System;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    [Serializable]
    public struct CellGenerationConfig
    {
        public CellTypeEnum cellType;
        public int weight;
    }

    public class SupernovaAuthoring : MonoBehaviour
    {
        [Header("超新星设置")] [SerializeField] private int mass = 100;
        [SerializeField] private int explosionStrength = 10;
        [SerializeField] private int explosionAngleClamp = 30;

        [Header("Cell 生成设置")] [SerializeField] private int generateRange = 10;
        [Range(0, 20)] [SerializeField] private float generateDensity = 10f;

        [SerializeField] private CellGenerationConfig[] cellGenerationConfigs;

        private class CenterAuthoringBaker : Baker<SupernovaAuthoring>
        {
            public override void Bake(SupernovaAuthoring authoring)
            {
                // 设置 Transform 依赖
                // 当 Transform 发生变化时触发 Bake
                DependsOn(authoring.transform);

                // 确保位置四舍五入为最接近的整数
                authoring.transform.position = new Vector3(
                    Mathf.RoundToInt(authoring.transform.position.x),
                    Mathf.RoundToInt(authoring.transform.position.y),
                    Mathf.RoundToInt(authoring.transform.position.z)
                );

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<SupernovaTag>(entity);
                AddComponent<ShouldInitializeCell>(entity);

                // 超新星设置
                AddComponent(entity, new Mass { Value = authoring.mass });
                AddComponent(entity, new ExplosionStrength { Value = authoring.explosionStrength });
                AddComponent(entity, new ExplosionAngleClamp { Value = authoring.explosionAngleClamp });

                // Cell 生成设置
                AddComponent(entity, new CellGenerateRange { Value = authoring.generateRange });
                AddComponent(entity, new CellGenerateDensity { Value = authoring.generateDensity });

                // 设置不同类型的 Cell 的生成权重
                var buffer = AddBuffer<CellGenerationConfigBuffer>(entity);
                foreach (var config in authoring.cellGenerationConfigs)
                {
                    buffer.Add(new CellGenerationConfigBuffer { CellType = config.cellType, Weight = config.weight });
                }
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