using System;
using UnityEngine.Profiling;

namespace E.Entities
{
    public struct EntityProfilerInfo
    {
#if ENABLE_PROFILER
        public EntityProfilerInfo(string name)
        {
            this.name = name;
        }

        internal readonly string name;
#else
        public EntityProfilerInfo(string name)
        {
        }
#endif
    }

    public struct EntityProfiler : IDisposable
    {
        public EntityProfiler(EntityProfilerInfo info)
        {
#if ENABLE_PROFILER
            Profiler.BeginSample(info.name);
#endif
        }

        public void Dispose()
        {
#if ENABLE_PROFILER
            Profiler.EndSample();
#endif
        }
    }
}