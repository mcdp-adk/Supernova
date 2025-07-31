using _Scripts.Utilities;
using Unity.Entities;

namespace _Scripts.Components
{
    // ========== Tag ==========

    public struct SupernovaTag : IComponentData
    {
    }

    public struct ShouldInitializeCell : IComponentData, IEnableableComponent
    {
    }

    // ========== Data ==========

    public struct ExplosionStrength : IComponentData
    {
        public int Value;
    }

    public struct ExplosionAngleClamp : IComponentData
    {
        public int Value;
    }

    public struct CellGenerateRange : IComponentData
    {
        public int Value;
    }

    public struct CellGenerateDensity : IComponentData
    {
        public float Value;
    }

    // ========== Buffer ==========

    public struct CellGenerationConfigBuffer : IBufferElementData
    {
        public CellTypeEnum CellType;
        public int Weight;
    }
}