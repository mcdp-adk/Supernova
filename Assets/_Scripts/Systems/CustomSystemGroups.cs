using Unity.Entities;

namespace _Scripts.Systems
{
    /// <summary>
    /// 初始化阶段的细胞自动机系统组
    /// 负责管理 Cell 的创建、实例化等初始化相关系统
    /// 在 Unity 的 InitializationSystemGroup 中执行
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializationCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// 可变频率细胞自动机系统组
    /// 负责管理 Cell 的生成、更新等变频执行的系统
    /// 在 Unity 的 VariableRateSimulationSystemGroup 中执行
    /// </summary>
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }
}