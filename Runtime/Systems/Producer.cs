using Unity.Jobs;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    internal struct Producer
    {
        public NativeEventStream EventStream;
        public JobHandle JobHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public bool HandleSet;
#endif
    }
}
