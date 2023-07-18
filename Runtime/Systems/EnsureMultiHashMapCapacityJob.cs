using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace BroWar.Events.Systems
{
    [BurstCompile]
    public struct EnsureMultiHashMapCapacityJob<TKey, TValue> : IJob
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
    {
        [ReadOnly]
        public NativeArray<int> Counter;

        public NativeParallelMultiHashMap<TKey, TValue> HashMap;

        public void Execute()
        {
            var count = 0;

            for (var i = 0; i < Counter.Length; i++)
            {
                count += Counter[i];
            }

            var requiredSize = HashMap.Count() + count;

            if (HashMap.Capacity < requiredSize)
            {
                HashMap.Capacity = requiredSize;
            }
        }
    }
}
