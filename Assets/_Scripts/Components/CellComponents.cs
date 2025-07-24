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

    public struct CellPendingDequeue : IComponentData, IEnableableComponent
    {
    }
    
    // 状态标识

    public struct IsCellAlive : IComponentData, IEnableableComponent
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
    
    // 物理属性
    
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
    
    public struct Temperature : IComponentData
    {
        public float Value;
    }
    
    // public struct Density : IComponentData
    // {
    //     public int Value;
    // }
    
    // public struct Hardness : IComponentData
    // {
    //     public float Value;
    // }
    
    // 化学属性

    public struct Energy : IComponentData
    {
        public float Value;
    }
    
    // public struct Wetness : IComponentData
    // {
    //     public float Value;
    // }

    // public struct Flammability : IComponentData
    // {
    //     public float Value;
    // }

    // ========== Buffer ==========

    public struct PendingChangeBuffer : IBufferElementData
    {
        public float3 VelocityDelta;
        public float TemperatureDelta;
        
        public float EnergyDelta;
    }
}