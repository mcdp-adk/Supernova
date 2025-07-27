using Unity.Entities;

namespace _Scripts.Utilities
{
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class CaSlowSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(GlobalConfig.SlowUpdateRateInMS);

            // 默认禁用该系统组，等待 GlobalDataSystem 完成初始化后再启用
            Enabled = false;
        }
    }
    
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class CaFastSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(GlobalConfig.FastUpdateRateInMS);

            // 默认禁用该系统组，等待 GlobalDataSystem 完成初始化后再启用
            Enabled = false;
        }
    }
}