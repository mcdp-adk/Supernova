using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Components
{
    public struct SpaceshipProxyTag : IComponentData
    {
    }
    
    public struct SpaceshipTempCellTag : IComponentData
    {
    }

    public struct SpaceshipMass : IComponentData
    {
        public int Value;
    }

    public struct SpaceshipVelocity : IComponentData
    {
        public float3 Value;
    }

    public struct SpaceshipColliderBuffer : IBufferElementData
    {
        public float3 Center;
        public float3 Size;
        public quaternion Rotation;
    }

    public struct SpaceshipForceFeedback : IComponentData
    {
        public float3 Value;
    }
}