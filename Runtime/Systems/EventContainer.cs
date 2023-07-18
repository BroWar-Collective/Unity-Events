using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    internal unsafe struct EventContainer : IDisposable
    {
        private readonly NativeList<IntPtr> producers;
        private readonly NativeList<IntPtr> consumers;
        private readonly NativeList<NativeEventStream> currentProducers;

        public EventContainer(long hash)
        {
            Hash = hash;
            producers = new NativeList<IntPtr>(1, Allocator.Persistent);
            consumers = new NativeList<IntPtr>(1, Allocator.Persistent);
            currentProducers = new NativeList<NativeEventStream>(1, Allocator.Persistent);
            IsValid = true;
        }

        private JobHandle GetProducerHandle()
        {
            var handles = new NativeList<JobHandle>(producers.Length, Allocator.Temp);

            for (var i = 0; i < producers.Length; i++)
            {
                var producer = (Producer*)producers[i];

                if (!producer->EventStream.IsCreated)
                {
                    continue;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!producer->HandleSet)
                {
                    Debug.LogError("CreateWriter must always be balanced by an AddJobHandle call.");
                    continue;
                }

                producer->HandleSet = false;
#endif

                handles.Add(producer->JobHandle);
            }

            return JobHandle.CombineDependencies(handles.AsArray());
        }

        private JobHandle GetConsumerHandle()
        {
            var handles = new NativeList<JobHandle>(consumers.Length, Allocator.Temp);

            for (var i = 0; i < consumers.Length; i++)
            {
                var consumer = (Consumer*)consumers[i];

                if (!consumer->Readers.IsCreated)
                {
                    continue;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (consumer->ReadersRequested != consumer->HandleSet)
                {
                    Debug.LogError("GetReaders must always be balanced by an AddJobHandle call.");
                    continue;
                }

                consumer->HandleSet = 0;
                consumer->ReadersRequested = 0;
#endif

                handles.Add(consumer->JobHandle);
            }

            return JobHandle.CombineDependencies(handles.AsArray());
        }

        private void RemoveProducerInternal(Producer* producer)
        {
            for (var i = 0; i < producers.Length; i++)
            {
                if (producers[i] == (IntPtr)producer)
                {
                    if (producer->EventStream.IsCreated)
                    {
                        producer->EventStream.Dispose();
                    }

                    producers.RemoveAtSwapBack(i);
                    break;
                }
            }

            UnsafeUtility.Free(producer, Allocator.Persistent);
        }

        private void RemoveConsumerInternal(Consumer* consumer)
        {
            for (var i = 0; i < consumers.Length; i++)
            {
                if (consumers[i] == (IntPtr)consumer)
                {
                    if (consumer->Readers.IsCreated)
                    {
                        consumer->Readers.Dispose();
                    }

                    consumers.RemoveAtSwapBack(i);
                    break;
                }
            }

            UnsafeUtility.Free(consumer, Allocator.Persistent);
        }

        public void Dispose()
        {
            for (var i = producers.Length - 1; i >= 0; i--)
            {
                RemoveProducerInternal((Producer*)producers[i]);
            }

            producers.Dispose();

            for (var i = consumers.Length - 1; i >= 0; i--)
            {
                RemoveConsumerInternal((Consumer*)consumers[i]);
            }

            consumers.Dispose();

            for (var i = 0; i < currentProducers.Length; i++)
            {
                currentProducers[i].Dispose();
            }

            currentProducers.Dispose();

            IsValid = false;
        }

        public EventProducer<T> CreateProducer<T>()
            where T : unmanaged
        {
            var producer = (Producer*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Producer>(), UnsafeUtility.AlignOf<Producer>(), Allocator.Persistent);
            UnsafeUtility.MemClear(producer, UnsafeUtility.SizeOf<Producer>());

            producers.Add((IntPtr)producer);

            return new EventProducer<T> { Producer = producer };
        }

        public EventConsumer<T> CreateConsumer<T>()
            where T : unmanaged
        {
            var consumer = (Consumer*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Consumer>(), UnsafeUtility.AlignOf<Consumer>(), Allocator.Persistent);
            UnsafeUtility.MemClear(consumer, UnsafeUtility.SizeOf<Consumer>());
            consumer->Readers = new NativeList<NativeEventStream>(0, Allocator.Persistent);

            consumers.Add((IntPtr)consumer);

            return new EventConsumer<T> { Consumer = consumer };
        }

        public void RemoveProducer<T>(EventProducer<T> producer)
            where T : unmanaged
        {
            RemoveProducerInternal(producer.Producer);
        }

        public void RemoveConsumer<T>(EventConsumer<T> consumer)
            where T : unmanaged
        {
            RemoveConsumerInternal(consumer.Consumer);
        }

        public void Update()
        {
            var producerHandle = GetProducerHandle();
            var consumerHandle = GetConsumerHandle();

            // NOTE: Dispose all current producers when all the consumers are finished
            for (var i = 0; i < currentProducers.Length; i++)
            {
                currentProducers[i].Dispose(consumerHandle);
            }

            currentProducers.Clear();

            // NOTE: Grab all new producers and reset them
            for (var i = 0; i < producers.Length; i++)
            {
                var producer = (Producer*)producers[i];
                if (producer->EventStream.IsCreated)
                {
                    currentProducers.Add(producer->EventStream);
                }

                producer->EventStream = default;
                producer->JobHandle = default;
            }

            // NOTE: Dispose our previous consumers and assign producers to our consumers
            for (var i = 0; i < consumers.Length; i++)
            {
                var consumer = (Consumer*)consumers[i];
                consumer->Readers.Clear();
                consumer->InputHandle = producerHandle;
                consumer->JobHandle = default;

                for (var r = 0; r < currentProducers.Length; r++)
                {
                    consumer->Readers.Add(currentProducers[r]);
                }
            }
        }

        public long Hash { get; }
        public bool IsValid { get; private set; }
    }
}
