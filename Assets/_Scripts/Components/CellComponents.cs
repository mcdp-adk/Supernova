using _Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Components
{
    // ========== Tag ==========

    // 基本标识

    public struct CellPrototypeTag : IComponentData
    {
    }

    public struct CellTag : IComponentData
    {
    }

    public struct PendingDequeue : IComponentData, IEnableableComponent
    {
    }

    // 状态标识

    public struct IsAlive : IComponentData, IEnableableComponent
    {
    }

    public struct IsBurning : IComponentData, IEnableableComponent
    {
    }

    public struct ShouldExplosion : IComponentData, IEnableableComponent
    {
    }

    // ========== Data ==========


    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }

    public struct CellState : IComponentData
    {
        public CellStateEnum Value;
    }

    public struct Mass : IComponentData
    {
        public int Value;
    }

    public struct Velocity : IComponentData, IEnableableComponent
    {
        public float3 Value;
    }

    public struct Temperature : IComponentData
    {
        public float Value;
    }

    public struct Moisture : IComponentData
    {
        public float Value;
    }

    public struct Energy : IComponentData
    {
        public float Value;
    }

    // ========== Buffer ==========

    public struct ImpulseBuffer : IBufferElementData
    {
        public float3 Value;
    }

    public struct HeatBuffer : IBufferElementData
    {
        public float Value;
    }

    public struct MoistureBuffer : IBufferElementData
    {
        public float Value;
    }
}