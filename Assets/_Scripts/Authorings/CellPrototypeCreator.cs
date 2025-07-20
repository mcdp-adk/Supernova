using System;
using System.Collections.Generic;
using _Scripts.Components;
using Unity.Collections;
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

            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                GetRenderMeshDescription(),
                GetRenderMeshArray(),
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            AddComponents(prototype, entityManager);
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

        private static void AddComponents(Entity prototype, EntityManager entityManager)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            ecb.AddComponent<CellPrototypeTag>(prototype);

            ecb.AddComponent<CellTag>(prototype);
            ecb.AddComponent<IsCellAlive>(prototype);
            ecb.AddComponent<CellType>(prototype);
            ecb.AddBuffer<PendingCellUpdateBuffer>(prototype);

            ecb.Playback(entityManager);
        }
    }
}