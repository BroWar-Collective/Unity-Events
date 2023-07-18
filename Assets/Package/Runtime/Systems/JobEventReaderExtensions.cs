using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Containers;
    using BroWar.Events.Streams;

    /// <summary>
    /// Extension methods for <see cref="IJobEventReader"/>.
    /// </summary>
    public static class JobEventReaderExtensions
    {
        /// <summary>
        /// Schedule a <see cref="IJobEventReader"/> job.
        /// </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="consumer"> The consumer. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the key in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle Schedule<TJob, T>(this TJob jobData, EventConsumer<T> consumer, JobHandle dependsOn = default)
            where TJob : struct, IJobEventReader
            where T : unmanaged
        {
            if (!consumer.HasReaders)
            {
                return dependsOn;
            }

            dependsOn = consumer.GetReaders(dependsOn, out var readers);

            for (var i = 0; i < readers.Length; i++)
            {
                var reader = readers[i];

                var fullData = new EventJobReaderStruct<TJob> { Reader = reader, JobData = jobData, Index = i };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                    UnsafeUtility.AddressOf(ref fullData),
                    EventJobReaderStruct<TJob>.Initialize(),
                    dependsOn,
                    ScheduleMode.Parallel);

                dependsOn = JobsUtility.Schedule(ref scheduleParams);
            }

            readers.Dispose(dependsOn);
            consumer.AddJobHandle(dependsOn);

            return dependsOn;
        }

        /// <summary>
        /// Schedule a <see cref="IJobEventReader"/> job.
        /// </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="readers"> The readers. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData, UnsafeReadArray<NativeEventStream.Reader> readers, JobHandle dependsOn = default)
            where TJob : struct, IJobEventReader
        {
            for (var i = 0; i < readers.Length; i++)
            {
                var fullData = new EventJobReaderStruct<TJob> { Reader = readers[i], JobData = jobData, Index = i };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                    UnsafeUtility.AddressOf(ref fullData),
                    EventJobReaderStruct<TJob>.Initialize(),
                    dependsOn,
                    ScheduleMode.Parallel);

                dependsOn = JobsUtility.Schedule(ref scheduleParams);
            }

            readers.Dispose(dependsOn);

            return dependsOn;
        }

        /// <summary>
        /// Schedule a <see cref="IJobEventReader"/> job.
        /// </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="consumer"> The consumer. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the key in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle ScheduleParallel<TJob, T>(
            this TJob jobData, EventConsumer<T> consumer, JobHandle dependsOn = default)
            where TJob : struct, IJobEventReader
            where T : unmanaged
        {
            if (!consumer.HasReaders)
            {
                return dependsOn;
            }

            dependsOn = consumer.GetReaders(dependsOn, out var readers);

            var input = dependsOn;

            for (var i = 0; i < readers.Length; i++)
            {
                var fullData = new EventJobReaderStruct<TJob> { Reader = readers[i], JobData = jobData, Index = i };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                    UnsafeUtility.AddressOf(ref fullData),
                    EventJobReaderStruct<TJob>.Initialize(),
                    input,
                    ScheduleMode.Parallel);

                var handle = JobsUtility.Schedule(ref scheduleParams);
                dependsOn = JobHandle.CombineDependencies(dependsOn, handle);
            }

            readers.Dispose(dependsOn);
            consumer.AddJobHandle(dependsOn);

            return dependsOn;
        }

        /// <summary>
        /// Schedule a <see cref="IJobEventReader"/> job.
        /// </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="readers"> The readers. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle ScheduleParallel<TJob>(
            this TJob jobData, UnsafeReadArray<NativeEventStream.Reader> readers, JobHandle dependsOn = default)
            where TJob : struct, IJobEventReader
        {
            var input = dependsOn;

            for (var i = 0; i < readers.Length; i++)
            {
                var fullData = new EventJobReaderStruct<TJob> { Reader = readers[i], JobData = jobData, Index = i };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                    UnsafeUtility.AddressOf(ref fullData),
                    EventJobReaderStruct<TJob>.Initialize(),
                    input,
                    ScheduleMode.Parallel);

                var handle = JobsUtility.Schedule(ref scheduleParams);
                dependsOn = JobHandle.CombineDependencies(dependsOn, handle);
            }

            readers.Dispose(dependsOn);

            return dependsOn;
        }
    }
}
