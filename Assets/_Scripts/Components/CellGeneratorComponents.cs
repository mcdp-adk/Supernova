using Unity.Entities;

namespace _Scripts.Components
{
    public struct CellGeneratorTag : IComponentData
    {
    }

    public struct ShouldInitializeCell : IComponentData, IEnableableComponent
    {
    }

    public struct CellGeneratorData : IComponentData
    {
        public int CoreRange;
        public Entity CellPrefab;
    }
}