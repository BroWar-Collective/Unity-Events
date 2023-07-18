using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;

namespace BroWar.Events.Systems
{
    using BroWar.Events.Streams;

    /// <summary>
    /// The job execution struct. 
    /// </summary>
    /// <typeparam name="TJob"> The type of the job. </typeparam>
    internal struct JobEventReaderForEachStructParallel<TJob>
            where TJob : struct, IJobEventReaderForEach
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

        /// <summary>
        /// The index of the reader.
        /// </summary>
        public int Index;

        // NOTE: ReSharper disable once StaticMemberInGenericType
        private static IntPtr jobReflectionData;

        private delegate void ExecuteJobFunction(
            ref JobEventReaderForEachStructParallel<TJob> fullData,
            IntPtr additionalPtr,
            IntPtr bufferRangePatchData,
            ref JobRanges ranges,
            int jobIndex);

        /// <summary>
        /// Initializes the job.
        /// </summary>
        /// <returns> The job pointer. </returns>
        public static IntPtr Initialize()
        {
            if (jobReflectionData == IntPtr.Zero)
            {
                jobReflectionData = JobsUtility.CreateJobReflectionData(
                    typeof(JobEventReaderForEachStructParallel<TJob>),
                    typeof(TJob),
                    (ExecuteJobFunction)Execute);
            }

            return jobReflectionData;
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
            ref JobEventReaderForEachStructParallel<TJob> fullData,
            IntPtr additionalPtr,
            IntPtr bufferRangePatchData,
            ref JobRanges ranges,
            int jobIndex)
        {
            while (true)
            {
                if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var begin, out var end))
                {
                    return;
                }

                for (var i = begin; i < end; i++)
                {
                    fullData.JobData.Execute(fullData.Reader, i);
                }
            }
        }
    }
}
