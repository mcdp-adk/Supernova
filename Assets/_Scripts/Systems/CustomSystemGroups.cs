using Unity.Entities;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializationCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial class VariableRateCellularAutomataSystemGroup : ComponentSystemGroup
    {
    }
}