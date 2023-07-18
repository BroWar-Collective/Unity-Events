using Unity.Burst;
using Unity.Collections;

namespace BroWar.Events.Systems
{
    [BurstCompile]
    public struct ToNativeListJob<T> : IJobEvent<T>
            where T : unmanaged
    {
        public NativeList<T> List;

        public void Execute(T e)
        {
            List.Add(e);
        }
    }
}
