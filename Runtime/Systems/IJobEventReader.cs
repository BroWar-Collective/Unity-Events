using System.Diagnostics.CodeAnalysis;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    /// <summary>
    /// Job that visits each event stream.
    /// </summary>
    [JobProducerType(typeof(EventJobReaderStruct<>))]
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Strict requirements for compiler")]
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Required by scheduler")]
    public interface IJobEventReader
    {
        /// <summary>
        /// Executes the next event.
        /// </summary>
        /// <param name="reader"> The stream. </param>
        /// <param name="readerIndex"> The reader index. </param>
        void Execute(NativeEventStream.Reader reader, int readerIndex);
    }
}
