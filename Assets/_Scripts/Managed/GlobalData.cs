using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Managed
{
    public class GlobalData : IComponentData, IDisposable
    {
        public NativeHashMap<int3, Entity> CellMap;

        public void Dispose()
        {
            if (!CellMap.IsCreated) return;
            CellMap.Dispose();
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GlobalDataInitSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // 确保单例只被创建一次
            var query = EntityManager.CreateEntityQuery(typeof(GlobalData));
            if (!query.IsEmpty) return;

            // 1. 创建一个单例实体
            var singletonEntity = EntityManager.CreateEntity();
            EntityManager.SetName(singletonEntity, "GlobalDataSingleton");

            // 2. 创建一个 NativeHashMap<int3, Entity> 并将其添加到单例实体上
            var initialMap = new NativeHashMap<int3, Entity>(1024, Allocator.Persistent);

            // 3. 初始化 NativeHashMap
            EntityManager.AddComponentObject(singletonEntity, new GlobalData
            {
                CellMap = initialMap
            });
        }

        protected override void OnUpdate()
        {
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            var query = EntityManager.CreateEntityQuery(typeof(GlobalData));
            if (query.IsEmpty) return;

            // 确保在销毁时正确释放 NativeHashMap
            var singletonEntity = query.GetSingletonEntity();
            var globalData = EntityManager.GetComponentObject<GlobalData>(singletonEntity);

            globalData.Dispose();
        }
    }
}