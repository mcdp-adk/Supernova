using Unity.Entities;

namespace _Scripts.Components
{
    public struct CellGeneratorTag : IComponentData
    {
    }

    public struct ShouldInitializeCell : IComponentData, IEnableableComponent
    {
    }

    public struct CellGenerateRange : IComponentData
    {
        public int Value;
    }

    public struct CellPrefabs : IComponentData
    {
        public Entity Value;
    }
}