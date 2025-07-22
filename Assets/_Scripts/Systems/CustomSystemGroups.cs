using _Scripts.Utilities;
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
    /// 固定频率细胞自动机系统组
    /// 负责管理 Cell 的生成、更新等系统，以固定频率执行
    /// 使用 VariableRateSimulationSystemGroup 和 VariableRateManager
    /// </summary>
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(GlobalConfig.UpdateRateInMS);
        }
    }
}