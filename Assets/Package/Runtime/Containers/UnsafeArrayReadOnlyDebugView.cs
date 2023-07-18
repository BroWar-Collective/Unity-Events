namespace BroWar.Events.Containers
{
    internal sealed class UnsafeArrayReadOnlyDebugView<T>
        where T : unmanaged
    {
        private UnsafeReadArray<T> m_Array;

        public UnsafeArrayReadOnlyDebugView(UnsafeReadArray<T> array) => m_Array = array;

        public T[] Items => m_Array.ToArray();
    }
}
