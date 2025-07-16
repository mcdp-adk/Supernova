using System;
using System.Collections.Generic;
using _Scripts.Components;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    [Serializable]
    public struct CellPrefabConfig
    {
        public GameObject prefab;
        public int weight;
    }

    public class SupernovaAuthoring : MonoBehaviour
    {
        [Header("超新星设置")] [SerializeField] private int mass = 100;

        [Header("Cell 生成设置")] [Tooltip("从中心到生成边缘的距离")] [SerializeField]
        private int generateRange;

        [Range(0, 100)] [SerializeField] private int generateDensity = 25;

        [SerializeField] private List<CellPrefabConfig> cellPrefabConfigs;

        private class CenterAuthoringBaker : Baker<SupernovaAuthoring>
        {
            public override void Bake(SupernovaAuthoring authoring)
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

                AddComponent(entity, new Mass { Value = authoring.mass });

                AddComponent(entity, new CellGenerateRange { Value = authoring.generateRange });
                AddComponent(entity, new CellGenerateDensity { Value = authoring.generateDensity });

                var cellsBuffer = AddBuffer<CellPrefabData>(entity);
                foreach (var config in authoring.cellPrefabConfigs)
                {
                    cellsBuffer.Add(new CellPrefabData
                    {
                        Prefab = GetEntity(config.prefab, TransformUsageFlags.Dynamic),
                        Weight = config.weight
                    });
                }
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