namespace E.Entities
{
    internal static class ProfilerInfos
    {
        public static readonly EntityProfilerInfo ExecuteProfilerInfo = new EntityProfilerInfo("Entity Systems");

        public static readonly EntityProfilerInfo CompleteProfilerInfo = new EntityProfilerInfo("Complete");

        public static readonly EntityProfilerInfo StartProfilerInfo = new EntityProfilerInfo("Start");

        public static readonly EntityProfilerInfo UpdateProfilerInfo = new EntityProfilerInfo("Update");

        public static EntityProfilerInfo[] EachSystemProfilerInfos;
    }
}