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
        }
    }
}