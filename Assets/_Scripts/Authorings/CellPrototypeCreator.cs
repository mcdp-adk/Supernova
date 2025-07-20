using System;
using System.Collections.Generic;
using _Scripts.Components;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Scripts.Authorings
{
    public struct CellPrototypeTag : IComponentData
    {
    }

    public class CellPrototypeCreator : MonoBehaviour
    {
        [Serializable]
        private struct CellPrefabConfig
        {
            public CellTypeEnum cellType;
            public Mesh cellMesh;
            public Material cellMaterial;
        }

        [SerializeField] private CellPrefabConfig[] cellPrefabConfigs;

        private RenderMeshArray _renderMeshArray;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var prototype = entityManager.CreateEntity();

            entityManager.SetName(prototype, "CellPrototype");

            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                GetRenderMeshDescription(),
                GetRenderMeshArray(),
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            entityManager.AddComponent<CellPrototypeTag>(prototype);
        }

        private static RenderMeshDescription GetRenderMeshDescription()
        {
            return new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.On,
                receiveShadows: true,
                motionVectorGenerationMode: MotionVectorGenerationMode.ForceNoMotion,
                layer: 0,
                renderingLayerMask: 1
            );
        }

        private RenderMeshArray GetRenderMeshArray()
        {
            var cellMeshes = new List<Mesh>();
            var cellMaterials = new List<Material>();
            foreach (var config in cellPrefabConfigs)
            {
                cellMeshes.Add(config.cellMesh);
                cellMaterials.Add(config.cellMaterial);
            }

            return new RenderMeshArray(cellMaterials.ToArray(), cellMeshes.ToArray());
        }
    }
}