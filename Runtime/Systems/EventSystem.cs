using Unity.Entities;

namespace BroWar.Events.Systems
{
    /// <summary>
    /// The default EventSystem that updates in the <see cref="LateSimulationSystemGroup"/>.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class EventSystem : EventSystemBase
    { }
}
