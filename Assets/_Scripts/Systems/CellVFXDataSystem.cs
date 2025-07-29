using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class CellVFXDataSystem : SystemBase
    {
        private static readonly int CellCountProperty = Shader.PropertyToID("CellCount");
        private static readonly int PositionBufferProperty = Shader.PropertyToID("PositionBuffer");
        private static readonly int VelocityBufferProperty = Shader.PropertyToID("VelocityBuffer");

        private VisualEffect _cellVFX;
        private GraphicsBuffer _positionBuffer;
        private GraphicsBuffer _velocityBuffer;

        protected override void OnCreate()
        {
            base.OnCreate();

            _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                GlobalConfig.MaxCellCount, sizeof(float) * 3);
            _velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                GlobalConfig.MaxCellCount, sizeof(float) * 3);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _positionBuffer?.Dispose();
            _velocityBuffer?.Dispose();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            _cellVFX = Object.FindAnyObjectByType<VisualEffect>();
        }

        protected override void OnUpdate()
        {
            var positionList = new NativeList<float3>(GlobalConfig.MaxCellCount, Allocator.TempJob);
            var velocityList = new NativeList<float3>(GlobalConfig.MaxCellCount, Allocator.TempJob);

            var bufferCollectionJob = new BufferCollectionJob
            {
                PositionList = positionList.AsParallelWriter(),
                VelocityList = velocityList.AsParallelWriter()
            };
            bufferCollectionJob.ScheduleParallel(Dependency).Complete();

            var cellCount = positionList.Length;

            _positionBuffer.SetData(positionList.AsArray());
            _velocityBuffer.SetData(velocityList.AsArray());

            _cellVFX.SetInt(CellCountProperty, cellCount);
            _cellVFX.SetGraphicsBuffer(PositionBufferProperty, _positionBuffer);
            _cellVFX.SetGraphicsBuffer(VelocityBufferProperty, _velocityBuffer);

            positionList.Dispose();
            velocityList.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct BufferCollectionJob : IJobEntity
        {
            public NativeList<float3>.ParallelWriter PositionList;
            public NativeList<float3>.ParallelWriter VelocityList;

            private void Execute(in LocalTransform transform, in Velocity velocity)
            {
                PositionList.AddNoResize(transform.Position);
                VelocityList.AddNoResize(-velocity.Value);
            }
        }
    }
}