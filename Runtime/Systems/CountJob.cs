using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    [BurstCompile]
    public struct CountJob : IJobEventReader
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> Counter;

        public void Execute(NativeEventStream.Reader reader, int readerIndex)
        {
            Counter[readerIndex] = reader.Count();
        }
    }
}
