using Unity.Collections;
using Unity.Jobs;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    internal struct Consumer
    {
        public NativeList<NativeEventStream> Readers;
        public JobHandle JobHandle;
        public JobHandle InputHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public int ReadersRequested;
        public int HandleSet;
#endif
    }
}
