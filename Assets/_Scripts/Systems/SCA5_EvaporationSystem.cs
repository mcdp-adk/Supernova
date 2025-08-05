using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaSlowSystemGroup))]
    [UpdateAfter(typeof(MoistureDiffusionSystem))]
    public partial struct EvaporationSystem : ISystem
    {
        private NativeArray<CellConfig> _cellConfigs;

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellConfigs.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellConfigs = globalDataSystem.CellConfigs;
            }

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new EvaporationJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);

            // 应用温度变化（包括蒸发导致的降温）
            state.Dependency = new TemperatureUpdateJob
            {
                CellConfigs = _cellConfigs
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            // 应用水分变化（包括蒸发导致的水分减少）
            state.Dependency = new MoistureUpdateJob().ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct EvaporationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int index, Entity entity,
                in Temperature temperature, in Moisture moisture)
            {
                // 只有当温度超过沸点且含有水分时才蒸发
                if (temperature.Value <= 100f || moisture.Value <= 0f)
                    return;

                // 线性蒸发：温度越高蒸发越快
                var temperatureDiff = temperature.Value - 100f;
                var evaporationRate = temperatureDiff * GlobalConfig.EvaporationCoefficient;
                var evaporationAmount = math.min(evaporationRate, moisture.Value);

                if (evaporationAmount <= 0f)
                    return;

                // 蒸发消耗热量（单位：J）
                var heatLoss = evaporationAmount * GlobalConfig.WaterLatentHeat;

                // 写入缓冲器
                ECB.AppendToBuffer(index, entity, new MoistureBuffer { Value = -evaporationAmount });
                ECB.AppendToBuffer(index, entity, new HeatBuffer { Value = -heatLoss });
            }
        }
    }
}