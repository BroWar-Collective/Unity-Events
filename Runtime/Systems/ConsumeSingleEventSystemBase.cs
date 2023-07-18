﻿namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    /// <summary> 
    /// A base system for working with jobs that have no extra data. 
    /// </summary>
    /// <typeparam name="T"> The job type. </typeparam>
    public abstract partial class ConsumeSingleEventSystemBase<T> : ConsumeEventSystemBase<T>
        where T : unmanaged
    {
        /// <summary> 
        /// Called when an event is read. 
        /// </summary>
        /// <param name="e"> The event. </param>
        protected abstract void OnEvent(T e);

        /// <inheritdoc/>
        protected sealed override void OnEventStream(ref NativeEventStream.Reader reader, int eventCount)
        {
            for (var j = 0; j < eventCount; j++)
            {
                var e = reader.Read<T>();
                OnEvent(e);
            }
        }
    }
}
