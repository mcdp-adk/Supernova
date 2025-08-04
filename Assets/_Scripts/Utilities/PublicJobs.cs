using _Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Utilities
{
    [BurstCompile]
    [WithAll(typeof(IsAlive))]
    [WithPresent(typeof(IsBurning), typeof(ShouldExplosion))]
    public partial struct TemperatureUpdateJob : IJobEntity
    {
        [ReadOnly] public NativeArray<CellConfig> CellConfigs;

        private void Execute(in CellType type, ref Temperature temperature, DynamicBuffer<HeatBuffer> heatBuffer,
            EnabledRefRW<IsBurning> isBurning, EnabledRefRW<ShouldExplosion> shouldExplosion)
        {
            // 更新温度
            for (var i = 0; i < heatBuffer.Length; i++)
                temperature.Value += heatBuffer[i].Value;
            heatBuffer.Clear();

            // 根据温度更新燃烧和爆炸状态
            var config = CellConfigs.GetCellConfig(type.Value);
            if (temperature.Value >= config.ExplosionPoint)
            {
                // 达到爆炸点：启用爆炸，禁用燃烧
                shouldExplosion.ValueRW = true;
                isBurning.ValueRW = false;
            }
            else if (temperature.Value >= config.IgnitionPoint)
            {
                // 达到燃点但未达到爆炸点：启用燃烧，禁用爆炸
                isBurning.ValueRW = true;
                shouldExplosion.ValueRW = false;
            }
            else
            {
                // 未达到燃点：禁用燃烧和爆炸
                isBurning.ValueRW = false;
                shouldExplosion.ValueRW = false;
            }
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
}