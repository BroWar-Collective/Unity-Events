using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace BroWar.Events.Streams
{
    [BurstCompile]
    public struct SumJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> Counter;

        [WriteOnly]
        public NativeArray<int> Count;

        public void Execute()
        {
            var count = 0;

            for (var i = 0; i < Counter.Length; i++)
            {
                count += Counter[i];
            }

            Count[0] = count;
        }
    }
}
