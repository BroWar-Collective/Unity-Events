using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Safety.")]
    public unsafe struct EventProducer<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal Producer* Producer;

        /// <summary>
        /// Create a new NativeEventStream to write events to.
        /// </summary>
        /// <typeparam name="T"> The type of event. </typeparam>
        /// <returns> A <see cref="NativeEventStream.Writer"/> you can write events to. </returns>
        /// <exception cref="InvalidOperationException"> Throw if unbalanced CreateEventWriter and AddJobHandleForProducer calls. </exception>
        public NativeEventStream.Writer CreateWriter()
        {
            Debug.Assert(!Producer->EventStream.IsCreated, "Creating multiple writers in same frame.");

            var eventStream = new NativeEventStream(Allocator.TempJob);
            Producer->EventStream = eventStream;
            return eventStream.AsWriter();
        }

        /// <summary>
        /// Create a new NativeEventStream to write events to returning the previous dependency.
        /// You can use this to write events in a fixed update.
        /// </summary>
        /// <param name="dependency"> The job handle to add. </param>
        /// <param name="writer"> The event writer. </param>
        /// <typeparam name="T"> The type of event. </typeparam>
        /// <returns> A <see cref="NativeEventStream.Writer"/> The new dependency. </returns>
        /// <exception cref="InvalidOperationException"> Throw if unbalanced CreateEventWriter and AddJobHandleForProducer calls. </exception>
        public JobHandle CreateWriter(JobHandle dependency, out NativeEventStream.Writer writer)
        {
            Producer->JobHandle = JobHandle.CombineDependencies(Producer->JobHandle, dependency);

            if (Producer->EventStream.IsCreated)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(Producer->HandleSet, "CreateWriter must always be balanced by an AddJobHandle call.");
                Producer->HandleSet = false;
#endif
                writer = Producer->EventStream.AsWriter();
                return Producer->JobHandle;
            }

            var eventStream = new NativeEventStream(Allocator.TempJob);
            Producer->EventStream = eventStream;
            writer = Producer->EventStream.AsWriter();
            return Producer->JobHandle;
        }

        /// <summary>
        /// Adds the specified JobHandle to the events list of producer dependency handles.
        /// </summary>
        /// <param name="handle"> The job handle to add. </param>
        /// <typeparam name="T"> The type of event to associate the handle to. </typeparam>
        /// <exception cref="InvalidOperationException"> Throw if unbalanced CreateEventWriter and AddJobHandleForProducer calls. </exception>
        public void AddJobHandle(JobHandle handle)
        {
            Producer->JobHandle = handle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Producer->HandleSet = true;
#endif
        }

        public bool IsValid => Producer != null;
    }
}
