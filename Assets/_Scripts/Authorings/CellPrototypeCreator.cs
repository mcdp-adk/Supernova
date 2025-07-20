using System;
using System.Collections.Generic;
using _Scripts.Components;
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
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var prototype = entityManager.CreateEntity();

            entityManager.SetName(prototype, "CellPrototype");

            // 使用 RenderMeshUtility 自动添加渲染需要的 Components
            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                GetRenderMeshDescription(),
                GetRenderMeshArray(),
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            // 添加其他 Components
            AddCellComponents(prototype, entityManager);
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
        
        private static void AddCellComponents(Entity prototype, EntityManager entityManager)
        {
            entityManager.AddComponent<CellPrototypeTag>(prototype);

            entityManager.AddComponent<CellTag>(prototype);
            entityManager.AddComponent<CellType>(prototype);
            entityManager.AddComponent<IsCellAlive>(prototype);
            entityManager.AddBuffer<PendingCellUpdateBuffer>(prototype);
        }
    }
}