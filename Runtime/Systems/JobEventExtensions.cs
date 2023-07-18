using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    /// <summary> 
    /// Extension methods for <see cref="IJobEvent{T}"/>.
    /// </summary>
    public static class JobEventExtensions
    {
        /// <summary> Schedule a <see cref="IJobEvent{T}"/> job. </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="consumer"> The consumer. </param>
        /// <param name="dependsOn"> T he job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the key in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static JobHandle Schedule<TJob, T>(this TJob jobData, EventConsumer<T> consumer, JobHandle dependsOn = default)
            where TJob : struct, IJobEvent<T>
            where T : unmanaged
        {
            return ScheduleInternal(jobData, consumer, dependsOn, false);
        }

        /// <summary> Schedule a <see cref="IJobEvent{T}"/> job. </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="consumer"> The consumer. </param>
        /// <param name="dependsOn"> T he job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the key in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static JobHandle ScheduleParallel<TJob, T>(this TJob jobData, EventConsumer<T> consumer, JobHandle dependsOn = default)
            where TJob : struct, IJobEvent<T>
            where T : unmanaged
        {
            return ScheduleInternal(jobData, consumer, dependsOn, true);
        }

        private static unsafe JobHandle ScheduleInternal<TJob, T>(this TJob jobData, EventConsumer<T> consumer, JobHandle dependsOn, bool isParallel)
            where TJob : struct, IJobEvent<T>
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

                var fullData = new JobEventProducer<TJob, T>
                {
                    Reader = reader,
                    JobData = jobData,
                    IsParallel = isParallel,
                };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                    UnsafeUtility.AddressOf(ref fullData),
                    isParallel ? JobEventProducer<TJob, T>.InitializeParallel() : JobEventProducer<TJob, T>.InitializeSingle(),
                    dependsOn,
                    ScheduleMode.Parallel);

                dependsOn = isParallel
                    ? JobsUtility.ScheduleParallelFor(ref scheduleParams, reader.ForEachCount, 1)
                    : JobsUtility.Schedule(ref scheduleParams);
            }

            readers.Dispose(dependsOn);
            consumer.AddJobHandle(dependsOn);

            return dependsOn;
        }
    }
}
