using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace BroWar.Events.Systems
{
    /// <summary> 
    /// The base EventSystem class. Implement this to add a new EventSystem for a specific group.
    /// </summary>
    public abstract partial class EventSystemBase : SystemBase
    {
        private NativeParallelHashMap<long, EventContainer> eventContainers;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSystemBase"/> class.
        /// </summary>
        [Preserve]
        protected EventSystemBase()
        {
            //NOTE: Done in constructor so other jobs can register / unregister in OnStart()
            eventContainers = new NativeParallelHashMap<long, EventContainer>(16, Allocator.Persistent);
        }

        private EventContainer GetOrCreateEventContainer<T>()
            where T : struct
        {
            var hash = Unity.Burst.BurstRuntime.GetHashCode64<T>();
            if (!eventContainers.TryGetValue(hash, out var container))
            {
                container = eventContainers[hash] = new EventContainer(hash);
            }

            return container;
        }

        private EventContainer GetEventContainer<T>()
             where T : struct
        {
            if (!eventContainers.IsCreated)
            {
                return default;
            }

            var hash = Unity.Burst.BurstRuntime.GetHashCode64<T>();
            return eventContainers.TryGetValue(hash, out var container)
                ? container
                : default;
        }

        // <inheritdoc/>
        protected override void OnDestroy()
        {
            using var e = eventContainers.GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Value.Dispose();
            }

            eventContainers.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var containers = eventContainers;
            Job.WithCode(() =>
            {
                using var e = containers.GetEnumerator();
                while (e.MoveNext())
                {
                    e.Current.Value.Update();
                }
            })
                 .WithoutBurst()
                 .Run();
        }

        /// <summary>
        /// Registers and allocates new event producer.
        /// </summary>
        /// <typeparam name="T"> The event type. </typeparam>
        /// <returns> The new allocated producer. </returns>
        public EventProducer<T> RegisterProducer<T>()
             where T : unmanaged
        {
            var container = GetOrCreateEventContainer<T>();
            return container.CreateProducer<T>();
        }

        /// <summary>
        /// Deregisters and frees the memory of an existing event producer.
        /// </summary>
        /// <param name="producer"> The producer to deregister. </param>
        /// <typeparam name="T"> The event type. </typeparam>
        public void DeregisterProducer<T>(EventProducer<T> producer)
             where T : unmanaged
        {
            //NOTE: If container doesn't exist it's because the EventSystem has already disposed it
            var container = GetEventContainer<T>();
            if (container.IsValid)
            {
                container.RemoveProducer(producer);
            }
        }

        /// <summary>
        /// Registers and allocates new event consumer.
        /// </summary>
        /// <typeparam name="T"> The event type. </typeparam>
        /// <returns> The new allocated consumer. </returns>
        public EventConsumer<T> RegisterConsumer<T>()
             where T : unmanaged
        {
            var container = GetOrCreateEventContainer<T>();
            return container.CreateConsumer<T>();
        }

        /// <summary>
        /// Deregisters and frees the memory of an existing event consumer.
        /// </summary>
        /// <param name="consumer"> The consumer to deregister. </param>
        /// <typeparam name="T"> The event type. </typeparam>
        public void DeregisterConsumer<T>(EventConsumer<T> consumer)
             where T : unmanaged
        {
            //NOTE: If container doesn't exist it's because the EventSystem has already disposed it
            var container = GetEventContainer<T>();
            if (container.IsValid)
            {
                container.RemoveConsumer(consumer);
            }
        }
    }
}