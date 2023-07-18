using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Streams
{
    /// <summary>
    /// A data streaming supporting parallel reading and parallel writings, without any thread safety check features.
    /// Allows you to write different types or arrays into a single stream.
    /// </summary>
    public unsafe partial struct UnsafeEventStream : INativeDisposable, IEquatable<UnsafeEventStream>
    {
        [NativeDisableUnsafePtrRestriction]
        private UnsafeEventStreamBlockData* blockData;

        private AllocatorManager.AllocatorHandle allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeEventStream" /> struct.
        /// </summary>
        /// <param name="allocator"> The specified type of memory allocation. </param>
        public UnsafeEventStream(Allocator allocator)
        {
            AllocateBlock(out this, allocator);
            AllocateForEach();
        }

        internal void Deallocate()
        {
            if (blockData == null)
            {
                return;
            }

            for (var i = 0; i != ForEachCount; i++)
            {
                var block = blockData->Blocks[i];
                while (block != null)
                {
                    var next = block->Next;
                    Memory.Unmanaged.Free(block, allocator);
                    block = next;
                }
            }

            Memory.Unmanaged.Free(blockData->Ranges, allocator);
            Memory.Unmanaged.Free(blockData, allocator);
            blockData = null;
            allocator = Allocator.None;
        }

        internal static void AllocateBlock(out UnsafeEventStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            var allocationSize = sizeof(UnsafeEventStreamBlockData) + (sizeof(UnsafeEventStreamBlock*) * ForEachCount);
            var buffer = (byte*)Memory.Unmanaged.Allocate(allocationSize, 16, allocator);
            UnsafeUtility.MemClear(buffer, allocationSize);

            var block = (UnsafeEventStreamBlockData*)buffer;

            stream.blockData = block;
            stream.allocator = allocator;

            block->Allocator = allocator;
            block->Blocks = (UnsafeEventStreamBlock**)(buffer + sizeof(UnsafeEventStreamBlockData));

            block->Ranges = null;
        }

        internal void AllocateForEach()
        {
            long allocationSize = sizeof(UnsafeEventStreamRange) * ForEachCount;
            blockData->Ranges = (UnsafeEventStreamRange*)Memory.Unmanaged.Allocate(allocationSize, 16, allocator);
            UnsafeUtility.MemClear(blockData->Ranges, allocationSize);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <returns> True if this container empty. </returns>
        public bool IsEmpty()
        {
            if (!IsCreated)
            {
                return true;
            }

            for (var i = 0; i != ForEachCount; i++)
            {
                if (blockData->Ranges[i].ElementCount > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns reader instance.
        /// </summary>
        /// <returns> Reader instance. </returns>
        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        /// <summary>
        /// Returns a writer instance.
        /// </summary>
        /// <returns> Writer instance. </returns>
        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        /// <summary>
        /// The current number of items in the container.
        /// </summary>
        /// <returns> The item count. </returns>
        public int Count()
        {
            var itemCount = 0;

            for (var i = 0; i != ForEachCount; i++)
            {
                itemCount += blockData->Ranges[i].ElementCount;
            }

            return itemCount;
        }

        /// <summary>
        /// Copies stream data into NativeArray.
        /// </summary>
        /// <typeparam name="T"> The type of value. </typeparam>
        /// <param name="arrayAllocator"> A member of the <see cref="Allocator" /> enumeration. </param>
        /// <returns> A new NativeArray, allocated with the given strategy and wrapping the stream data. </returns>
        /// <remarks>
        /// <para> The array is a copy of stream data. </para>
        /// </remarks>
        [GenerateTestsForBurstCompatibility]
        public NativeArray<T> ToNativeArray<T>(Allocator arrayAllocator)
            where T : struct
        {
            var array = new NativeArray<T>(Count(), arrayAllocator, NativeArrayOptions.UninitializedMemory);
            var reader = AsReader();

            var offset = 0;
            for (var i = 0; i != reader.ForEachCount; i++)
            {
                reader.BeginForEachIndex(i);
                var rangeItemCount = reader.RemainingItemCount;
                for (var j = 0; j < rangeItemCount; ++j)
                {
                    array[offset] = reader.Read<T>();
                    offset++;
                }

                reader.EndForEachIndex();
            }

            return array;
        }

        /// <summary>
        /// Disposes of this stream and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            Deallocate();
        }

        /// <inheritdoc />
        public bool Equals(UnsafeEventStream other)
        {
            return blockData == other.blockData;
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the <see cref="JobHandle" /> returned by the Job.Schedule method using the `jobHandle` parameter so the job scheduler can dispose the container
        /// after all jobs using it have run.
        /// </para>
        /// </remarks>
        /// <param name="inputDeps"> All jobs spawned will depend on this JobHandle. </param>
        /// <returns>
        /// A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.
        /// </returns>
        [GenerateTestsForBurstCompatibility /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new DisposeUnsafeEventStreamJob { Container = this }.Schedule(inputDeps);
            blockData = null;
            return jobHandle;
        }

        /// <summary> 
        /// Gets the number of streams the list can use.
        /// </summary>
        public static int ForEachCount => JobsUtility.ThreadIndexCount;

        /// <summary>
        /// Gets a value indicating whether memory for the container is allocated.
        /// </summary>
        /// <value> True if this container object's internal storage has been allocated. </value>
        /// <remarks>
        /// <para>
        /// Note that the container storage is not created if you use the default constructor.
        /// You must specify at least an allocation type to construct a usable container.
        /// </para>
        /// </remarks>
        public bool IsCreated => blockData != null;
    }
}
