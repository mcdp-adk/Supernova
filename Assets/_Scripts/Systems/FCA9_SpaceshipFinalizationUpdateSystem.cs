using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup), OrderLast = true)]
    public partial struct SpaceshipFinalizationUpdateSystem : ISystem
    {
        private EntityQuery _bufferQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bufferQuery = SystemAPI.QueryBuilder().WithAll<SpaceshipTempCellTag, ImpulseBuffer>().Build();
            state.RequireForUpdate<SpaceshipProxyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 收集所有 TempCell 的 Impulse
            var totalImpulse = float3.zero;

            // 使用传统的查询方式获取 DynamicBuffer
            var tempCells = _bufferQuery.ToEntityArray(Allocator.Temp);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var entity in tempCells)
            {
                var impulseBuffer = SystemAPI.GetBuffer<ImpulseBuffer>(entity);

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var impulse in impulseBuffer) totalImpulse += impulse.Value;

                // 读取完毕后清理 buffer
                impulseBuffer.Clear();
            }

            // 将总 Impulse 写入 SpaceshipForceFeedback
            SystemAPI.SetSingleton(new SpaceshipForceFeedback { Value = totalImpulse });
        }
    }
}