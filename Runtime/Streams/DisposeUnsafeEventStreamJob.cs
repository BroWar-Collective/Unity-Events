using Unity.Burst;
using Unity.Jobs;

namespace BroWar.Events.Streams
{
    [BurstCompile]
    internal struct DisposeUnsafeEventStreamJob : IJob
    {
        public UnsafeEventStream Container;

        public void Execute()
        {
            Container.Deallocate();
        }
    }
}
