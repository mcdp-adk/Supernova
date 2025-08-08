using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup))]
    [UpdateAfter(typeof(SpaceshipVoxelizationSystem))]
    public partial struct SpaceshipCollisionPredictionSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpaceshipProxyTag>();
            state.RequireForUpdate<SpaceshipMass>();
            state.RequireForUpdate<SpaceshipVelocity>();
            state.RequireForUpdate<SpaceshipForceFeedback>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            // 获取飞船数据
            var colliderBuffer = SystemAPI.GetSingletonBuffer<SpaceshipColliderBuffer>();
            var spaceshipMass = SystemAPI.GetSingleton<SpaceshipMass>().Value;
            var spaceshipVelocity = SystemAPI.GetSingleton<SpaceshipVelocity>().Value;

            var displacement = math.length(spaceshipVelocity * GlobalConfig.FastUpdateRateInMS / 1000f);
            while (displacement > 0)
            {
                displacement -= 0.1f;
            }
        }
    }
}