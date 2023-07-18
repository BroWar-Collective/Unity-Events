using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    /// <summary>
    /// The parallel job execution struct.
    /// </summary>
    /// <typeparam name="TJob"> The type of the job. </typeparam>
    /// <typeparam name="T"> The type of the event. </typeparam>
    internal struct JobEventProducer<TJob, T>
            where TJob : struct, IJobEvent<T>
            where T : unmanaged
    {
        /// <summary>
        /// The <see cref="NativeEventStream.Reader"/>.
        /// </summary>
        [ReadOnly]
        public NativeEventStream.Reader Reader;

        /// <summary>
        /// The job.
        /// </summary>
        public TJob JobData;

        public bool IsParallel;

        // NOTE: ReSharper disable once StaticMemberInGenericType
        private static IntPtr jobReflectionDataSingle;

        // NOTE: ReSharper disable once StaticMemberInGenericType
        private static IntPtr jobReflectionDataParallel;

        private delegate void ExecuteJobFunction(
            ref JobEventProducer<TJob, T> fullData,
            IntPtr additionalPtr,
            IntPtr bufferRangePatchData,
            ref JobRanges ranges,
            int jobIndex);

        /// <summary>
        /// Initializes the job.
        /// </summary>
        /// <returns> The job pointer. </returns>
        public static IntPtr InitializeSingle()
        {
            if (jobReflectionDataSingle == IntPtr.Zero)
            {
                jobReflectionDataSingle = JobsUtility.CreateJobReflectionData(
                    typeof(JobEventProducer<TJob, T>),
                    typeof(TJob),
                    (ExecuteJobFunction)Execute);
            }

            return jobReflectionDataSingle;
        }

        /// <summary>
        /// Initializes the job.
        /// </summary>
        /// <returns> The job pointer. </returns>
        public static IntPtr InitializeParallel()
        {
            if (jobReflectionDataParallel == IntPtr.Zero)
            {
                jobReflectionDataParallel = JobsUtility.CreateJobReflectionData(
                    typeof(JobEventProducer<TJob, T>),
                    typeof(TJob),
                    (ExecuteJobFunction)Execute);
            }

            return jobReflectionDataParallel;
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        /// <param name="fullData"> The job data. </param>
        /// <param name="additionalPtr"> AdditionalPtr. </param>
        /// <param name="bufferRangePatchData"> BufferRangePatchData. </param>
        /// <param name="ranges"> The job range. </param>
        /// <param name="jobIndex"> The job index. </param>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by burst.")]
        public static void Execute(
            ref JobEventProducer<TJob, T> fullData,
            IntPtr additionalPtr,
            IntPtr bufferRangePatchData,
            ref JobRanges ranges,
            int jobIndex)
        {
            while (true)
            {
                var begin = 0;
                var end = fullData.Reader.ForEachCount;

                // If we are running the job in parallel, steal some work.
                if (fullData.IsParallel)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }
                }

                for (var i = begin; i < end; i++)
                {
                    var count = fullData.Reader.BeginForEachIndex(i);

                    for (var j = 0; j < count; j++)
                    {
                        var e = fullData.Reader.Read<T>();
                        fullData.JobData.Execute(e);
                    }

                    fullData.Reader.EndForEachIndex();
                }

                if (!fullData.IsParallel)
                {
                    break;
                }
            }
        }
    }
}