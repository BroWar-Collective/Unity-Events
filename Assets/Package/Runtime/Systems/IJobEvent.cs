using System.Diagnostics.CodeAnalysis;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    /// <summary> 
    /// Job that visits each event. 
    /// </summary>
    /// <typeparam name="T"> Type of event. </typeparam>
    [JobProducerType(typeof(JobEventProducer<,>))]
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Strict requirements for compiler")]
    public interface IJobEvent<T>
        where T : unmanaged
    {
        /// <summary> 
        /// Executes the next event.
        /// </summary>
        /// <param name="e"> The event. </param>
        void Execute(T e);
    }
}
