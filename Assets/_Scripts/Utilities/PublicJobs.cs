using _Scripts.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Utilities
{
    [BurstCompile]
    [WithAll(typeof(IsAlive))]
    public partial struct TemperatureUpdateJob : IJobEntity
    {
        private static void Execute(ref Temperature temperature, DynamicBuffer<HeatBuffer> heatBuffer)
        {
            for (var i = 0; i < heatBuffer.Length; i++)
                temperature.Value += heatBuffer[i].Value;
            heatBuffer.Clear();
        }
    }

    [BurstCompile]
    [WithAll(typeof(IsAlive))]
    public partial struct MoistureUpdateJob : IJobEntity
    {
        private static void Execute(ref Moisture moisture, DynamicBuffer<MoistureBuffer> moistureBuffer)
        {
            for (var i = 0; i < moistureBuffer.Length; i++)
                moisture.Value = math.clamp(moisture.Value + moistureBuffer[i].Value, 0f, 1f);
            moistureBuffer.Clear();
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