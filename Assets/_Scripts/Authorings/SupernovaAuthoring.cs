using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    public class SupernovaAuthoring : MonoBehaviour
    {
        [Header("超新星设置")] [SerializeField] private int mass = 100;

        [Header("Cell 分层生成设置")] [SerializeField]
        private LayerGenerationConfig[] layerConfigs;

        private class CenterAuthoringBaker : Baker<SupernovaAuthoring>
        {
            public override void Bake(SupernovaAuthoring authoring)
            {
                DependsOn(authoring.transform);

                authoring.transform.position = new Vector3(
                    Mathf.RoundToInt(authoring.transform.position.x),
                    Mathf.RoundToInt(authoring.transform.position.y),
                    Mathf.RoundToInt(authoring.transform.position.z)
                );

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<SupernovaTag>(entity);
                AddComponent<ShouldInitializeCell>(entity);
                AddComponent(entity, new Mass { Value = authoring.mass });

                var layerBuffer = AddBuffer<LayerGenerationConfigBuffer>(entity);
                var cellConfigBuffer = AddBuffer<LayerCellGenerationConfigBuffer>(entity);

                if (authoring.layerConfigs == null) return;
                for (var layerIndex = 0; layerIndex < authoring.layerConfigs.Length; layerIndex++)
                {
                    var layer = authoring.layerConfigs[layerIndex];

                    layerBuffer.Add(new LayerGenerationConfigBuffer
                    {
                        Radius = layer.radius,
                        Density = layer.density,
                        ExplosionStrength = layer.explosionStrength,
                        ExplosionAngleClamp = layer.explosionAngleClamp
                    });

                    if (layer.cellConfigs == null) continue;
                    foreach (var cellConfig in layer.cellConfigs)
                    {
                        cellConfigBuffer.Add(new LayerCellGenerationConfigBuffer
                        {
                            CellType = cellConfig.cellType,
                            Weight = cellConfig.weight,
                            LayerIndex = layerIndex
                        });
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (layerConfigs == null) return;

            Color[] colors = { Color.red, Color.yellow, Color.green, Color.blue, Color.magenta, Color.cyan };

            for (var i = 0; i < layerConfigs.Length; i++)
            {
                Gizmos.color = colors[i % colors.Length];
                Gizmos.DrawWireSphere(transform.position, layerConfigs[i].radius);
            }
        }
    }
}