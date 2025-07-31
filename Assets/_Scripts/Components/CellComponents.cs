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

    // ========== Data ==========

    // 基本属性

    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }

    public struct CellState : IComponentData
    {
        public CellStateEnum Value;
    }

    // 物理 - 静态属性

    public struct Mass : IComponentData
    {
        public int Value;
    }

    // 物理 - 动态属性

    public struct Velocity : IComponentData, IEnableableComponent
    {
        public float3 Value;
    }

    public struct Temperature : IComponentData
    {
        public float Value;
    }

    // 化学属性

    // 化学 - 动态属性

    public struct Energy : IComponentData
    {
        public float Value;
    }

    // ========== Buffer ==========

    public struct ImpulseBuffer : IBufferElementData
    {
        public float3 Value;
    }
}