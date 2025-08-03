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

    // ========== Buffer ==========
    
    public struct LayerGenerationConfigBuffer : IBufferElementData
    {
        public int Radius;
        public float Density;
        public int ExplosionStrength;
        public int ExplosionAngleClamp;
    }
    
    public struct LayerCellGenerationConfigBuffer : IBufferElementData
    {
        public CellTypeEnum CellType;
        public int Weight;
        public int LayerIndex;
    }
}