using _Scripts.Components;
using Unity.Mathematics;

namespace _Scripts.Utilities
{
    public enum CellTypeEnum
    {
        None = 0,
        Cell1 = -1,
        Cell2 = -2
    }
    
    public struct CellData
    {
        public CellTypeEnum CellType;
        public int3 Coordinate;
    }
}