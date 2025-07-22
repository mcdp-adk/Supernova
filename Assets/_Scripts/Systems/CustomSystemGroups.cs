using _Scripts.Utilities;
using Unity.Entities;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(GlobalConfig.UpdateRateInMS);

            // 默认禁用该系统组，等待 GlobalDataSystem 完成初始化后再启用
            Enabled = false;
        }
    }
}