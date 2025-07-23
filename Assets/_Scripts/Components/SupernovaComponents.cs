using _Scripts.Utilities;
using Unity.Entities;

namespace _Scripts.Components
{
    // ========== 标识组件 ==========
    
    public struct SupernovaTag : IComponentData
    {
    }
    
    public struct ShouldInitializeCell : IComponentData, IEnableableComponent
    {
    }

    // ========== 属性组件 ==========
    
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

    // ========== 配置缓冲 ==========
    
    public struct CellConfigBuffer : IBufferElementData
    {
        public CellTypeEnum CellType;
        public int Weight;
    }
}