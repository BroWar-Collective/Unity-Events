using System.Diagnostics.CodeAnalysis;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    /// <summary>
    /// Job that visits each index in each event stream.
    /// </summary>
    [JobProducerType(typeof(JobEventReaderForEachStructParallel<>))]
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Strict requirements for compiler")]
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Required by scheduler")]
    public interface IJobEventReaderForEach
    {
        /// <summary>
        /// Executes the next event.
        /// </summary>
        /// <param name="stream"> The stream. </param>
        /// <param name="foreachIndex"> The foreach index. </param>
        void Execute(NativeEventStream.Reader stream, int foreachIndex);
    }
}
