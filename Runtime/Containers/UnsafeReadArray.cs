using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace BroWar.Events.Containers
{
    [DebuggerTypeProxy(typeof(UnsafeArrayReadOnlyDebugView<>))]
    [DebuggerDisplay("Length = {Length}")]
    public unsafe struct UnsafeReadArray<T> : IDisposable
        where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        private void* m_Buffer;

        private int m_Length;
        private Allocator m_AllocatorLabel;

        internal UnsafeReadArray(void* buffer, int length, Allocator allocator)
        {
            m_Buffer = buffer;
            m_Length = length;
            m_AllocatorLabel = allocator;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if ((IntPtr)m_Buffer == IntPtr.Zero)
            {
                throw new ObjectDisposedException("The ReadArray is already disposed.");
            }

            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The ReadArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Buffer = null;
            m_Length = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The ReadArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if ((IntPtr)m_Buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("The ReadArray is already disposed.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                var jobHandle = new DisposeReadArrayJob
                {
                    Data = new ReadArrayDispose
                    {
                        m_Buffer = m_Buffer,
                        m_AllocatorLabel = m_AllocatorLabel,
                    },
                }.Schedule(inputDeps);

                m_Buffer = null;
                m_Length = 0;
                m_AllocatorLabel = Allocator.Invalid;
                return jobHandle;
            }

            m_Buffer = null;
            m_Length = 0;
            return inputDeps;
        }

        internal T[] ToArray()
        {
            var dst = new T[m_Length];
            var gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(
                (void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject()),
                (void*)((IntPtr)m_Buffer),
                m_Length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();

            return dst;
        }

        public T this[int index]
        {
            get
            {
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementReadAccess(int index)
        {
            if (index < 0 || index >= m_Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {m_Length - 1}).");
            }
        }

        public bool IsValid => m_Buffer != null;
        public int Length => m_Length;
    }
}
