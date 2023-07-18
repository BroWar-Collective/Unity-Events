using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BroWar.Events.Containers
{
    internal unsafe struct ReadArrayDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;
        internal Allocator m_AllocatorLabel;

        public void Dispose() => UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
    }
}
