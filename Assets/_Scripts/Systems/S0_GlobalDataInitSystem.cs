using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GlobalDataInitSystem : SystemBase
    {
        public NativeHashMap<int3, Entity> CellMap { get; private set; }
        public NativeQueue<Entity> CellPoolQueue { get; private set; }
        public NativeArray<CellConfig> CellConfigs;

        protected override void OnCreate()
        {
            CellMap = new NativeHashMap<int3, Entity>(GlobalConfig.CellMapInitialCapacity, Allocator.Persistent);
            CellPoolQueue = new NativeQueue<Entity>(Allocator.Persistent);
            
            // 确保存在所需的实体
            RequireForUpdate<CellConfigBuffer>();
            RequireForUpdate<CellPrototypeTag>();
        }

        protected override void OnUpdate()
        {
            // 初始化 CellConfigs
            var buffer = SystemAPI.GetSingletonBuffer<CellConfigBuffer>();
            CellConfigs = new NativeArray<CellConfig>(buffer.Length, Allocator.Persistent);
            for (var i = 0; i < buffer.Length; i++)
                CellConfigs[i] = buffer[i].Data;

            // 直接获取原型并创建实体池
            var prototype = SystemAPI.GetSingletonEntity<CellPrototypeTag>();
            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);
            
            for (var i = 0; i < GlobalConfig.MaxCellPoolSize; i++)
                CellUtility.InstantiateFromPrototype(prototype, ecb);
                
            ecb.Playback(EntityManager);
            
            Debug.Log("[GlobalDataInitSystem] 数据初始化完成");
            Enabled = false; // 完成后自禁用
        }

        protected override void OnDestroy()
        {
            if (CellMap.IsCreated) CellMap.Dispose();
            if (CellPoolQueue.IsCreated) CellPoolQueue.Dispose();
            if (CellConfigs.IsCreated) CellConfigs.Dispose();
        }
    }
}