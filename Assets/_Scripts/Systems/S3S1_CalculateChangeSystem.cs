using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Entities;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CellApplyChangeSystemGroup))]
    public partial struct CalculateChangeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 确保系统只在有 IsCellAlive 组件时更新
            state.RequireForUpdate<IsCellAlive>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 计算变化
            var calculateChangeJob = new CalculateChangeJob();
            state.Dependency = calculateChangeJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        [WithAll(typeof(IsCellAlive))]
        private partial struct CalculateChangeJob : IJobEntity
        {
            private void Execute(CellAspect cell)
            {
                foreach (var data in cell.PendingChangeBuffer)
                {
                    cell.Velocity += data.VelocityDelta;
                    cell.Temperature += data.TemperatureDelta;
                    cell.Energy += data.EnergyDelta;
                }
            }
        }
    }
}