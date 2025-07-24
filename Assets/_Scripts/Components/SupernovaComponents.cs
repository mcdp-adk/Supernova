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
        public float Value;
    }

    // ========== Buffer ==========
    
    public struct CellConfigBuffer : IBufferElementData
    {
        public CellTypeEnum CellType;
        public int Weight;
    }
}