using _Scripts.Components;
using Unity.Burst;
using Unity.Entities;

namespace _Scripts.Utilities
{
    [BurstCompile]
    [WithAll(typeof(IsAlive))]
    public partial struct TemperatureUpdateJob : IJobEntity
    {
        private void Execute()
        {
        }
    }

    [BurstCompile]
    [WithAll(typeof(IsAlive))]
    public partial struct MoistureUpdateJob : IJobEntity
    {
        private void Execute()
        {
        }
    }

    [BurstCompile]
    [WithAll(typeof(IsAlive))]
    public partial struct EnergyUpdateJob : IJobEntity
    {
        private void Execute()
        {
        }
    }
}