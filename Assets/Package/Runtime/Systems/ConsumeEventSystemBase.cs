using Unity.Entities;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    /// <summary> 
    /// A base system for working with jobs on the main thread.
    /// </summary>
    /// <typeparam name="T"> The job type. </typeparam>
    public abstract partial class ConsumeEventSystemBase<T> : SystemBase
        where T : unmanaged
    {
        private EventSystem eventSystem;
        private EventConsumer<T> consumer;

        /// <inheritdoc />
        protected sealed override void OnCreate()
        {
            eventSystem = World.GetExistingSystemManaged<EventSystem>();
            consumer = eventSystem.RegisterConsumer<T>();

            Create();
        }

        /// <inheritdoc />
        protected sealed override void OnDestroy()
        {
            eventSystem.DeregisterConsumer(consumer);
            Destroy();
        }

        /// <inheritdoc />
        protected sealed override void OnUpdate()
        {
            BeforeEvent();

            if (!consumer.HasReaders)
            {
                return;
            }

            Dependency = consumer.GetReaders(Dependency, out var readers);
            Dependency.Complete();

            try
            {
                for (var i = 0; i < readers.Length; i++)
                {
                    var reader = readers[i];

                    for (var foreachIndex = 0; foreachIndex < reader.ForEachCount; foreachIndex++)
                    {
                        var events = reader.BeginForEachIndex(foreachIndex);
                        OnEventStream(ref reader, events);
                        reader.EndForEachIndex();
                    }
                }
            }
            finally
            {
                consumer.AddJobHandle(Dependency);
            }
        }

        /// <summary>
        /// Optional create that can occur after system creation.
        /// </summary>
        protected virtual void Create()
        { }

        /// <summary>
        /// Optional destroy that can occur after system creation.
        /// </summary>
        protected virtual void Destroy()
        { }

        /// <summary>
        /// Optional update that can occur before event reading.
        /// </summary>
        protected virtual void BeforeEvent()
        { }

        /// <summary> 
        /// A stream of events. 
        /// </summary>
        /// <param name="reader"> The event stream reader. </param>
        /// <param name="eventCount"> The number of iterations in the stream. </param>
        protected abstract void OnEventStream(ref NativeEventStream.Reader reader, int eventCount);
    }
}
