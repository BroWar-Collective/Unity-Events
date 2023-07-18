using Unity.Collections;

namespace BroWar.Events.Streams
{
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeEventStreamRange
    {
        internal UnsafeEventStreamBlock* Block;
        internal int OffsetInFirstBlock;
        internal int ElementCount;

        // NOTE: One byte past the end of the last byte written
        internal int LastOffset;
        internal int NumberOfBlocks;

        internal UnsafeEventStreamBlock* CurrentBlock;
        internal byte* CurrentPtr;
        internal byte* CurrentBlockEnd;
    }
}
