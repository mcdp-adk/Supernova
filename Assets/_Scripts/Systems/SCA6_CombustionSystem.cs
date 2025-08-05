using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup))]
    [UpdateAfter(typeof(EvaporationSystem))]
    public partial struct CombustionSystem : ISystem
    {
        private NativeArray<CellConfig> _cellConfigs;
        private NativeHashMap<int3, Entity> _cellMap;

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellConfigs.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellConfigs = globalDataSystem.CellConfigs;
            }

            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            using var ecb1 = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new CombustionJob
            {
                ECB = ecb1.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb1.Playback(state.EntityManager);

            using var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new EnergyCheckJob
            {
                ECB = ecb2,
                CellMap = _cellMap,
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb2.Playback(state.EntityManager);
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(IsBurning))]
        private partial struct CombustionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform, 
                in Temperature temperature, ref Energy energy)
            {
                // 计算燃烧速率：基础速率 * (1 + 温度系数 * 温度)
                var combustionRate = GlobalConfig.CombustionBaseRate *
                                     (1f + GlobalConfig.CombustionTemperatureFactor * temperature.Value);

                // 实际燃烧能量消耗
                var actualConsumption = math.min(energy.Value, combustionRate);

                // 计算释放的热量
                var heatReleased = actualConsumption * GlobalConfig.CombustionHeatCoefficient;

                // 剩余能量
                var remainingEnergy = energy.Value - actualConsumption;

                // 更新能量值
                energy.Value = remainingEnergy;

                // 添加热量到 HeatBuffer
                ECB.AppendToBuffer(index, entity, new HeatBuffer { Value = heatReleased });
            }
        }
    }
}