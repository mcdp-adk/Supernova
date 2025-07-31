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

            // Buffer
            manager.AddBuffer<ImpulseBuffer>(prototype);
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
            NativeHashMap<int3, Entity> cellMap,
            CellTypeEnum cellType, int3 targetCoordinate, float3 velocity, float temperature, Entity configEntity)
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

            // 设置固定属性
            ecb.AddComponent<CellState>(cell);
            ecb.SetComponent(cell, new CellState { Value = config.State });

            ecb.AddComponent<Mass>(cell);
            ecb.SetComponent(cell, new Mass { Value = config.Mass });

            ecb.AddComponent<Energy>(cell);
            ecb.SetComponent(cell, new Energy { Value = 100f });

            // 设置可变属性
            ecb.AddComponent<Velocity>(cell);
            ecb.SetComponent(cell, new Velocity { Value = velocity });

            ecb.AddComponent<Temperature>(cell);
            ecb.SetComponent(cell, new Temperature { Value = temperature });

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

        public static bool TryMoveCell(Entity cell, ref LocalTransform localTransform,
            NativeHashMap<int3, Entity> cellMap, int3 targetCoordinate)
        {
            if (!cellMap.TryAdd(targetCoordinate, cell)) return false;

            cellMap.Remove((int3)localTransform.Position);
            localTransform.Position = targetCoordinate;
            localTransform.Rotation = quaternion.identity;
            localTransform.Scale = GlobalConfig.DefaultCellScale;

            return true;
        }

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