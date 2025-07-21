using Unity.Entities;

namespace _Scripts.Components
{
    // ========== Supernova 标识组件 ==========
    
    /// <summary>
    /// Cell 生成器标记 - 标识能够生成 Cell 的 Supernova 实体
    /// </summary>
    public struct SupernovaTag : IComponentData
    {
    }

    /// <summary>
    /// 初始化触发器 - 控制是否应该开始生成 Cell
    /// </summary>
    public struct ShouldInitializeCell : IComponentData, IEnableableComponent
    {
    }

    // ========== Supernova 属性组件 ==========
    
    /// <summary>
    /// 质量 - 影响 Supernova 的重力和影响范围
    /// </summary>
    public struct Mass : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Cell 生成范围 - 定义在多大半径内生成 Cell
    /// </summary>
    public struct CellGenerateRange : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Cell 生成密度 - 控制生成 Cell 的概率 (0-100%)
    /// </summary>
    public struct CellGenerateDensity : IComponentData
    {
        public float Value;
    }

    // ========== Supernova 配置缓冲区 ==========
    
    /// <summary>
    /// Cell 配置缓冲区 - 存储不同 Cell 类型的生成权重配置
    /// </summary>
    public struct CellConfigBuffer : IBufferElementData
    {
        public CellTypeEnum CellType;
        public int Weight;
    }
}