using Unity.Entities;

namespace _Scripts.Components
{
    public enum CellTypeEnum
    {
        None = 0
    }

    public struct CellTag : IComponentData
    {
    }

    public struct IsCellAlive : IComponentData, IEnableableComponent
    {
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