using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Components
{
    public enum CellTypeEnum
    {
        None = 0,
        Cell1 = -1,
        Cell2 = -2
    }

    public struct CellPrototypeTag : IComponentData
    {
    }

    public struct CellTag : IComponentData
    {
    }

    public struct CellType : IComponentData
    {
        public CellTypeEnum Value;
    }

    public struct IsCellAlive : IComponentData, IEnableableComponent
    {
    }

    public struct CellPosition : IComponentData
    {
        public int3 Value;
    }

    public struct PendingCellUpdateBuffer : IBufferElementData
    {
        public bool TargetAliveState;
    }
}