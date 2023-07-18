using System;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BroWar.Events.Streams
{
    public unsafe partial struct NativeEventStream
    {
        /// <summary>
        /// The reader instance.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Reader
        {
            private UnsafeEventStream.Reader reader;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private int remainingBlocks;
#pragma warning disable SA1308
            private readonly AtomicSafetyHandle m_Safety;
#pragma warning restore SA1308
#endif

            internal Reader(ref NativeEventStream stream)
            {
                reader = stream.stream.AsReader();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                remainingBlocks = 0;
                m_Safety = stream.m_Safety;
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckEndForEachIndex()
            {
                if (reader.m_RemainingItemCount != 0)
                {
                    throw new ArgumentException(
                        "Not all elements (Count) have been read. If this is intentional, simply skip calling EndForEachIndex();");
                }

                if (reader.m_CurrentBlockEnd != reader.m_CurrentPtr)
                {
                    throw new ArgumentException(
                        "Not all data (Data Size) has been read. If this is intentional, simply skip calling EndForEachIndex();");
                }
            }

            /// <summary> Begin reading data at the iteration index. </summary>
            /// <param name="foreachIndex"> The index to start reading. </param>
            /// <returns> The number of elements at this index. </returns>
            public int BeginForEachIndex(int foreachIndex)
            {
                CheckBeginForEachIndex(foreachIndex);

                var remainingItemCount = reader.BeginForEachIndex(foreachIndex);
                remainingItemCount = CollectionHelper.AssumePositive(remainingItemCount);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                remainingBlocks = reader.m_BlockStream->Ranges[foreachIndex].NumberOfBlocks;
                if (remainingBlocks == 0)
                {
                    reader.m_CurrentBlockEnd = (byte*)reader.m_CurrentBlock + reader.m_LastBlockSize;
                }
#endif

                return remainingItemCount;
            }

            /// <summary> Ensures that all data has been read for the active iteration index. </summary>
            /// <remarks> EndForEachIndex must always be called balanced by a BeginForEachIndex. </remarks>
            public void EndForEachIndex()
            {
                reader.EndForEachIndex();
                CheckEndForEachIndex();
            }

            /// <summary> Returns pointer to data. </summary>
            /// <param name="size"> The size of the data to read. </param>
            /// <returns> The pointer to the data. </returns>
            public byte* ReadUnsafePtr(int size)
            {
                CheckReadSize(size);

                reader.m_RemainingItemCount--;

                var ptr = reader.m_CurrentPtr;
                reader.m_CurrentPtr += size;

                if (reader.m_CurrentPtr > reader.m_CurrentBlockEnd)
                {
                    reader.m_CurrentBlock = reader.m_CurrentBlock->Next;
                    reader.m_CurrentPtr = reader.m_CurrentBlock->Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    remainingBlocks--;

                    CheckNotReadingOutOfBounds(size);

                    if (remainingBlocks <= 0)
                    {
                        reader.m_CurrentBlockEnd = (byte*)reader.m_CurrentBlock + reader.m_LastBlockSize;
                    }
                    else
                    {
                        reader.m_CurrentBlockEnd = (byte*)reader.m_CurrentBlock + UnsafeEventStreamBlockData.AllocationSize;
                    }
#else
                    reader.m_CurrentBlockEnd = (byte*)reader.m_CurrentBlock + UnsafeEventStreamBlockData.AllocationSize;
#endif
                    ptr = reader.m_CurrentPtr;
                    reader.m_CurrentPtr += size;
                }

                return ptr;
            }

            /// <summary> 
            /// Read data. 
            /// </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> The returned data. </returns>
            public ref T Read<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(ReadUnsafePtr(size));
            }

            /// <summary>
            /// The current number of items in the container.
            /// </summary>
            /// <returns> The item count. </returns>
            public int Count()
            {
                CheckRead();
                return reader.Count();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckNotReadingOutOfBounds(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (remainingBlocks < 0)
                {
                    throw new ArgumentException("Reading out of bounds");
                }

                if ((remainingBlocks == 0) && (size + sizeof(void*) > reader.m_LastBlockSize))
                {
                    throw new ArgumentException("Reading out of bounds");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckReadSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                Assert.IsTrue(size <= UnsafeEventStreamBlockData.AllocationSize - sizeof(void*));
                if (reader.m_RemainingItemCount < 1)
                {
                    throw new ArgumentException("There are no more items left to be read.");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckBeginForEachIndex(int forEachIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                if ((uint)forEachIndex >= (uint)ForEachCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(forEachIndex),
                        $"foreachIndex: {forEachIndex} must be between 0 and ForEachCount: {ForEachCount}");
                }
#endif
            }

            /// <summary>
            /// Gets the for each count.
            /// </summary>
            public int ForEachCount => reader.ForEachCount;

            /// <summary>
            /// Gets the remaining item count.
            /// </summary>
            public int RemainingItemCount => CollectionHelper.AssumePositive(reader.RemainingItemCount);
        }
    }
}
