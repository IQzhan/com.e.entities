namespace E.Entities
{
    public enum ScheduleMode
    {
        /// <summary>
        /// Run job immediately on calling thread.
        /// </summary>
        Run = 0,

        /// <summary>
        /// Schedule job to run on multiple worker threads if possible. 
        /// Jobs that cannot run concurrently will run on one thread only.
        /// </summary>
        Parallel = 1,

        /// <summary>
        /// Schedule job to run on a single worker thread.
        /// </summary>
        Single = 2
    }
}