using Unity.Jobs;

namespace BroWar.Events.Containers
{
    internal struct DisposeReadArrayJob : IJob
    {
        internal ReadArrayDispose Data;

        public void Execute() => Data.Dispose();
    }
}
