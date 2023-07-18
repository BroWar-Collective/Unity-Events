using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BroWar.Events.Streams
{
    public unsafe partial struct UnsafeEventStream
    {
        [GenerateTestsForBurstCompatibility]
        public struct Reader
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeEventStreamBlockData* m_BlockStream;

            [NativeDisableUnsafePtrRestriction]
            internal UnsafeEventStreamBlock* m_CurrentBlock;

            [NativeDisableUnsafePtrRestriction]
            internal byte* m_CurrentPtr;

            [NativeDisableUnsafePtrRestriction]
            internal byte* m_CurrentBlockEnd;

            internal int m_RemainingItemCount;
            internal int m_LastBlockSize;

            internal Reader(ref UnsafeEventStream stream)
            {
                m_BlockStream = stream.blockData;
                m_CurrentBlock = null;
                m_CurrentPtr = null;
                m_CurrentBlockEnd = null;
                m_RemainingItemCount = 0;
                m_LastBlockSize = 0;
            }

            /// <summary> Begin reading data at the iteration index. </summary>
            /// <param name="foreachIndex"> </param>
            /// <remarks> BeginForEachIndex must always be called balanced by a EndForEachIndex. </remarks>
            /// <returns> The number of elements at this index. </returns>
            public int BeginForEachIndex(int foreachIndex)
            {
                m_RemainingItemCount = m_BlockStream->Ranges[foreachIndex].ElementCount;
                m_LastBlockSize = m_BlockStream->Ranges[foreachIndex].LastOffset;

                m_CurrentBlock = m_BlockStream->Ranges[foreachIndex].Block;
                m_CurrentPtr = (byte*)m_CurrentBlock + m_BlockStream->Ranges[foreachIndex].OffsetInFirstBlock;
                m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeEventStreamBlockData.AllocationSize;

                return m_RemainingItemCount;
            }

            /// <summary>
            /// Ensures that all data has been read for the active iteration index.
            /// </summary>
            /// <remarks> EndForEachIndex must always be called balanced by a BeginForEachIndex. </remarks>
            public void EndForEachIndex()
            { }

            /// <summary>
            /// Returns for each count.
            /// </summary>
            public int ForEachCount => UnsafeEventStream.ForEachCount;

            /// <summary>
            /// Returns remaining item count.
            /// </summary>
            public int RemainingItemCount => m_RemainingItemCount;

            /// <summary>
            /// Returns pointer to data.
            /// </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to data. </returns>
            public byte* ReadUnsafePtr(int size)
            {
                m_RemainingItemCount--;

                var ptr = m_CurrentPtr;
                m_CurrentPtr += size;

                if (m_CurrentPtr > m_CurrentBlockEnd)
                {
                    m_CurrentBlock = m_CurrentBlock->Next;
                    m_CurrentPtr = m_CurrentBlock->Data;

                    m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeEventStreamBlockData.AllocationSize;

                    ptr = m_CurrentPtr;
                    m_CurrentPtr += size;
                }

                return ptr;
            }

            /// <summary>
            /// Read data.
            /// </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to data. </returns>
            [GenerateTestsForBurstCompatibility]
            public ref T Read<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(ReadUnsafePtr(size));
            }

            /// <summary>
            /// Peek into data.
            /// </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to data. </returns>
            [GenerateTestsForBurstCompatibility]
            public ref T Peek<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();

                var ptr = m_CurrentPtr;
                if (ptr + size > m_CurrentBlockEnd)
                {
                    ptr = m_CurrentBlock->Next->Data;
                }

                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            /// <summary>
            /// The current number of items in the container.
            /// </summary>
            /// <returns> The item count. </returns>
            public int Count()
            {
                var itemCount = 0;
                for (var i = 0; i != UnsafeEventStream.ForEachCount; i++)
                {
                    itemCount += m_BlockStream->Ranges[i].ElementCount;
                }

                return itemCount;
            }
        }
    }
}
