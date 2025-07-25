namespace _Scripts.Utilities
{
    public enum CellTypeEnum
    {
        None = 0,
        Cell1 = -1,
        Cell2 = -2
    }

    public enum CellStateEnum : byte
    {
        None = 0,
        Solid = 1,
        Liquid = 2,
        Gas = 3,
        Powder = 4,
    }
}