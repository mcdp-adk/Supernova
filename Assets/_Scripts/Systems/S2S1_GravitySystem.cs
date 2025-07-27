using _Scripts.Aspects;
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
    public partial struct GravitySystem : ISystem
    {
        private struct SupernovaData
        {
            public int3 Coordinate;
            public int Mass;
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取超新星数据
            var supernovaDataList = new NativeList<SupernovaData>(Allocator.TempJob);
            foreach (var supernova in SystemAPI.Query<SupernovaAspect>())
                supernovaDataList.Add(new SupernovaData
                {
                    Coordinate = supernova.Coordinate,
                    Mass = supernova.Mass
                });

            var gravityJob = new GravityJob
            {
                SupernovaDataArray = supernovaDataList.AsArray()
            };

            state.Dependency = gravityJob.Schedule(state.Dependency);
            state.Dependency.Complete();
            supernovaDataList.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct GravityJob : IJobEntity
        {
            [ReadOnly] public NativeArray<SupernovaData> SupernovaDataArray;

            private void Execute(RefRO<Mass> mass, RefRO<LocalTransform> localTransform,
                DynamicBuffer<ImpulseBuffer> impulseBuffer)
            {
                var cellPosition = (int3)localTransform.ValueRO.Position;
                var cellMass = mass.ValueRO.Value;

                foreach (var supernova in SupernovaDataArray)
                {
                    var direction = supernova.Coordinate - cellPosition;
                    var distanceSquared = math.lengthsq(direction);

                    // 如果距离过近则跳过，避免除以零或过大冲量
                    if (!(distanceSquared > 0.1f)) continue;
                    var impulseMagnitude = supernova.Mass * cellMass / distanceSquared;
                    var impulse = math.normalize(direction) * impulseMagnitude;
                    impulseBuffer.Add(new ImpulseBuffer { Value = impulse });
                }
            }
        }
    }
}