using Unity.Entities;

namespace _Scripts.Components
{
    public struct CellGeneratorTag : IComponentData
    {
    }

    public struct ShouldInitializeCell : IComponentData, IEnableableComponent
    {
    }

    public struct Mass : IComponentData
    {
        public int Value;
    }

    public struct CellGenerateRange : IComponentData
    {
        public int Value;
    }

    public struct CellGenerateDensity : IComponentData
    {
        public int Value;
    }
}