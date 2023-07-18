using Unity.Collections;
using UnityEngine;

namespace BroWar.Events.Streams
{
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeEventStreamBlockData
    {
        internal const int AllocationSize = 4 * 1024;

        internal UnsafeEventStreamBlock** Blocks;
        internal UnsafeEventStreamRange* Ranges;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal UnsafeEventStreamBlock* Allocate(UnsafeEventStreamBlock* oldBlock, int threadIndex)
        {
            Debug.Assert((threadIndex < UnsafeEventStream.ForEachCount) && (threadIndex >= 0));

            var block = (UnsafeEventStreamBlock*)Memory.Unmanaged.Allocate(AllocationSize, 16, Allocator);
            block->Next = null;

            if (oldBlock == null)
            {
                // NOTE: Append our new block in front of the previous head.
                block->Next = Blocks[threadIndex];
                Blocks[threadIndex] = block;
            }
            else
            {
                block->Next = oldBlock->Next;
                oldBlock->Next = block;
            }

            return block;
        }
    }
}
