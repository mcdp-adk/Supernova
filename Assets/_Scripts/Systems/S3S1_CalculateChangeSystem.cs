using _Scripts.Aspects;
using _Scripts.Components;
using Unity.Burst;
using Unity.Entities;

namespace _Scripts.Systems
{
    public partial struct CalculateChangeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
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
                
            }
        }
    }
}