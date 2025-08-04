using System.Collections.Generic;
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

            // Tag Components
            manager.AddComponent<CellPrototypeTag>(prototype);
            manager.AddComponent<CellTag>(prototype);
            manager.AddComponent<PendingDequeue>(prototype);
            manager.SetComponentEnabled<PendingDequeue>(prototype, false);
            manager.AddComponent<IsAlive>(prototype);
            manager.SetComponentEnabled<IsAlive>(prototype, false);

            // Data Components
            manager.AddComponent<CellType>(prototype);
            manager.AddComponent<CellState>(prototype);
            manager.AddComponent<Mass>(prototype);
            manager.AddComponent<Velocity>(prototype);
            manager.SetComponentEnabled<Velocity>(prototype, false);
            manager.AddComponent<Temperature>(prototype);
            manager.AddComponent<Moisture>(prototype);
            manager.AddComponent<Energy>(prototype);

            // Buffer Components
            manager.AddBuffer<ImpulseBuffer>(prototype);
            manager.AddBuffer<HeatBuffer>(prototype);
            manager.AddBuffer<MoistureBuffer>(prototype);
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

        #endregion

        public static bool TryAddCellToWorld(Entity cell, EntityManager manager, EntityCommandBuffer ecb,
            NativeHashMap<int3, Entity> cellMap, Entity configEntity,
            CellTypeEnum cellType, int3 targetCoordinate, float3 initialImpulse)
        {
            if (cellType == CellTypeEnum.None) return true;
            if (!cellMap.TryAdd(targetCoordinate, cell)) return false;

            ecb.AddComponent<LocalTransform>(cell);
            ecb.SetComponent(cell, new LocalTransform
            {
                Position = targetCoordinate,
                Rotation = quaternion.identity,
                Scale = GlobalConfig.DefaultCellScale
            });

            var config = new CellConfig();
            var buffer = manager.GetBuffer<CellConfigBuffer>(configEntity);
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var configBuffer in buffer)
            {
                if (configBuffer.Data.Type == cellType) config = configBuffer.Data;
            }

            ecb.SetComponentEnabled<IsAlive>(cell, true);
            ecb.SetComponent(cell, new CellType { Value = cellType });
            ecb.SetComponent(cell, new CellState { Value = config.State });
            ecb.SetComponent(cell, new Mass { Value = config.Mass });
            ecb.SetComponent(cell, new Velocity { Value = float3.zero, MovementDebt = float3.zero });
            ecb.SetComponent(cell, new Temperature { Value = config.TemperatureDefault });
            ecb.SetComponent(cell, new Moisture { Value = config.MoistureDefault });
            ecb.SetComponent(cell, new Energy { Value = config.EnergyDefault });
            ecb.SetComponent(cell, new MaterialMeshInfo
            {
                MaterialID = new BatchMaterialID { value = (uint)cellType },
                MeshID = new BatchMeshID { value = (uint)cellType }
            });

            // 设置 Buffer
            var impulseBuffer = ecb.SetBuffer<ImpulseBuffer>(cell);
            impulseBuffer.Clear();
            impulseBuffer.Add(new ImpulseBuffer { Value = initialImpulse });
            ecb.SetBuffer<HeatBuffer>(cell).Clear();
            ecb.SetBuffer<MoistureBuffer>(cell).Clear();

            return true;
        }

        public static void SetCellType(Entity cell, EntityCommandBuffer ecb, NativeHashMap<int3, Entity> cellMap,
            CellConfig config, int3 currentCoordinate, CellTypeEnum targetCellType)
        {
            if (targetCellType == CellTypeEnum.None)
            {
                cellMap.Remove(currentCoordinate);
                ecb.SetComponentEnabled<IsAlive>(cell, false);
                ecb.SetComponentEnabled<PendingDequeue>(cell, true);
            }
            else
            {
                ecb.SetComponent(cell, new CellType { Value = targetCellType });
                ecb.SetComponent(cell, new CellState { Value = config.State });
                ecb.SetComponent(cell, new Mass { Value = config.Mass });
                ecb.SetComponent(cell, new MaterialMeshInfo
                {
                    MaterialID = new BatchMaterialID { value = (uint)targetCellType },
                    MeshID = new BatchMeshID { value = (uint)targetCellType }
                });
            }
        }

        public static CellConfig GetCellConfig(this NativeArray<CellConfig> cellConfigs, CellTypeEnum cellType)
        {
            for (var i = 0; i < cellConfigs.Length; i++)
            {
                if (cellConfigs[i].Type == cellType)
                    return cellConfigs[i];
            }

            return default;
        }
    }
}