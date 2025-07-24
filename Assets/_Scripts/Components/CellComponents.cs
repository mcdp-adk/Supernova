using _Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Components
{
    // ========== Tag ==========

    public struct CellPrototypeTag : IComponentData
    {
    }

    public struct CellTag : IComponentData
    {
    }

    public struct CellPendingDequeue : IComponentData, IEnableableComponent
    {
    }

    public struct IsCellAlive : IComponentData, IEnableableComponent
    {
    }

    // ========== Data ==========

    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }
    
    public struct LifeTime : IComponentData
    {
        public int Value;
    }

    public struct Density : IComponentData
    {
        public int Value;
    }
    
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
    
    public struct Temperature : IComponentData
    {
        public float Value;
    }

    // ========== Buffer ==========

    public struct PendingCellUpdateBuffer : IBufferElementData
    {
        public bool TargetAliveState;
    }
}