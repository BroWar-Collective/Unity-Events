using Unity.Entities;

namespace BroWar.Events.Systems
{
    /// <summary>
    /// EventSystem that updates in the <see cref="FixedStepSimulationSystemGroup"/>.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class FixedStepEventSystem : EventSystemBase
    { }
}
