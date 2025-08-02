using System.Collections.Generic;
using System.Linq;
using _Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace _Scripts.Utilities
{
    public static class CellUtility
    {
        #region Initialize Preperations

        public static void CreatePrototype(string prototypeName, EntityManager manager,
            RenderMeshDescription description, RenderMeshArray renderMeshArray)
        {
            var prototype = manager.CreateEntity();

            manager.SetName(prototype, prototypeName);

            RenderMeshUtility.AddComponents(
                prototype,
                manager,
                description,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            // Tag
            manager.AddComponent<CellPrototypeTag>(prototype);
            manager.AddComponent<CellTag>(prototype);
            manager.AddComponent<PendingDequeue>(prototype);
            manager.SetComponentEnabled<PendingDequeue>(prototype, false);
            manager.AddComponent<IsAlive>(prototype);
            manager.SetComponentEnabled<IsAlive>(prototype, false);

            // Data
            manager.AddComponent<CellType>(prototype);
        }

        public static Entity CreateCellConfigEntity(string entityName, EntityManager manager,
            List<CellConfig> cellConfigs)
        {
            var configEntity = manager.CreateEntity();
            manager.SetName(configEntity, entityName);
            manager.AddComponent<CellConfigTag>(configEntity);

            var buffer = manager.AddBuffer<CellConfigBuffer>(configEntity);
            foreach (var cellConfig in cellConfigs)
                buffer.Add(new CellConfigBuffer { Data = cellConfig });

            return configEntity;
        }

        public static void InstantiateFromPrototype(Entity prototype, EntityCommandBuffer ecb)
        {
            var cell = ecb.Instantiate(prototype);

            ecb.SetName(cell, "");
            ecb.RemoveComponent<CellPrototypeTag>(cell);
            ecb.SetComponentEnabled<PendingDequeue>(cell, true);
        }

        public static void EnqueueCellIntoPool(Entity cell, EntityCommandBuffer ecb, NativeQueue<Entity> queue)
        {
            queue.Enqueue(cell);
            ecb.SetComponentEnabled<PendingDequeue>(cell, false);
        }

        #endregion

        #region Add Cell to World

        public static bool TryAddCellToWorld(Entity cell, EntityManager manager, EntityCommandBuffer ecb,
            NativeHashMap<int3, Entity> cellMap, Entity configEntity,
            CellTypeEnum cellType, int3 targetCoordinate, float3 initialImpulse)
        {
            if (!cellMap.TryAdd(targetCoordinate, cell)) return false;

            ecb.AddComponent<LocalTransform>(cell);
            ecb.SetComponent(cell, new LocalTransform
            {
                Position = targetCoordinate,
                Rotation = quaternion.identity,
                Scale = GlobalConfig.DefaultCellScale
            });

            SetCellType(cell, ecb, cellType);

            var config = GetCellConfig(manager, configEntity, cellType);
            ecb.AddComponent(cell, new CellState { Value = config.State });
            ecb.AddComponent(cell, new Mass { Value = config.Mass });
            ecb.AddComponent(cell, new Velocity { Value = float3.zero });
            ecb.SetComponentEnabled<Velocity>(cell, false);
            ecb.AddComponent(cell, new Temperature { Value = config.TemperatureDefault });
            ecb.AddComponent(cell, new Moisture { Value = config.MoistureDefault });
            ecb.AddComponent(cell, new Energy { Value = config.EnergyDefault });
            ecb.AddBuffer<ImpulseBuffer>(cell).Add(new ImpulseBuffer { Value = initialImpulse });

            return true;
        }

        private static void SetCellType(Entity cell, EntityCommandBuffer ecb, CellTypeEnum targetCellType)
        {
            ecb.SetComponentEnabled<IsAlive>(cell, targetCellType != CellTypeEnum.None);

            // targetCellType = CellTypeEnum.None;

            ecb.SetComponent(cell, new CellType { Value = targetCellType });
            ecb.SetComponent(cell, new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                MeshID = new BatchMeshID { value = (uint)targetCellType }
            });
        }

        #endregion

        public static CellConfig GetCellConfig(EntityManager manager, Entity configEntity, CellTypeEnum cellType)
        {
            var buffer = manager.GetBuffer<CellConfigBuffer>(configEntity);
            foreach (var configBuffer in buffer)
            {
                if (configBuffer.Data.Type == cellType)
                    return configBuffer.Data;
            }

            throw new System.InvalidOperationException($"未找到 CellType: {cellType} 的配置");
        }
    }
}