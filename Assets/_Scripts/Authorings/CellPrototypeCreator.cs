using System;
using System.Collections.Generic;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Scripts.Authorings
{
    public class CellPrototypeCreator : MonoBehaviour
    {
        private static CellPrototypeCreator Instance { get; set; }

        [Serializable]
        private struct CellPrefabConfig
        {
            public CellTypeEnum cellType;
            public Mesh cellMesh;
            public Material cellMaterial;
        }

        [SerializeField] private CellPrefabConfig[] cellPrefabConfigs;

        private RenderMeshArray _renderMeshArray;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            CellUtility.CreatePrototype("Cell_Prototype", World.DefaultGameObjectInjectionWorld.EntityManager,
                GetRenderMeshDescription(), GetRenderMeshArray());
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