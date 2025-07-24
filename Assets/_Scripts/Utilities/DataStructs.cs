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
        Solid = 0,
        Liquid = 1,
        Gas = 2,
        Powder = 3,
    }
}