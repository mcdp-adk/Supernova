using Unity.Entities;

namespace _Scripts.Components
{
    public enum CellTypeEnum : int
    {
        None = 0
    }

    public struct CellTag : IComponentData
    {
    }

    public struct IsCellAlive : IComponentData, IEnableableComponent
    {
        public bool Value;
    }

    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }

    public struct PendingCellUpdateBuffer : IBufferElementData
    {
        public bool TargetAliveState;
    }
}