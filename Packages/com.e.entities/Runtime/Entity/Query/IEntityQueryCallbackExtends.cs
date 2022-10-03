using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace E.Entities
{
    internal unsafe static class IEntityQueryCallbackExtends
    {
        public static JobHandle Schedule<T>(ref this T jobData, ref ContainerQueryParams containerQueryParams, ScheduleMode scheduleMode, JobHandle dependsOn = new JobHandle())
            where T : struct, IEntityQueryCallback
        {
            if (scheduleMode == ScheduleMode.Run)
            {
                dependsOn.Complete();
                fixed (ContainerQueryParams* ptr = &containerQueryParams)
                {
                    jobData.Execute(ptr);
                }
                return dependsOn;
            }
            int innerloopBatchCount = (scheduleMode == ScheduleMode.Single) ? containerQueryParams.length : 64;
            void* listData = QueryJobAdditionalDataMemory.Require();
            *(ContainerQueryParams*)listData = containerQueryParams;
            var scheduleParams = new JobsUtility.JobScheduleParameters
                (UnsafeUtility.AddressOf(ref jobData), JobStruct<T>.jobReflectionData, dependsOn, (Unity.Jobs.LowLevel.Unsafe.ScheduleMode)scheduleMode);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, listData, null);
        }

        private static void Execute<T>(ref this T jobData, ContainerQueryParams* containerQueryParams)
            where T : struct, IEntityQueryCallback
        {
            var container = containerQueryParams->container;
            var containerID = container->ID;
            var entitySize = container->entitySize;
            int begin = 0;
            int end = containerQueryParams->length;
            while (container->GetRange(ref begin, ref end,
                out EntityChunk* chunk, out int chunkStartInclude,
                out int innerStartInclude, out int innerEndExclude))
            {
                byte* chunkData = chunk->data;
                int innerEnd = innerEndExclude;
                int chunkStart = chunkStartInclude;
                for (int i = innerStartInclude; i < innerEnd; i++)
                {
                    var entityData = (EntityData*)(chunkData + i * entitySize);
                    Entity entity = new Entity(containerID, entityData->InnerKey, chunkStart + i);
                    QueryResult queryResult = new QueryResult(entityData, entity, containerQueryParams);
                    jobData.Execute(ref queryResult);
                }
            }
        }

        public class JobStruct<Callback>
            where Callback : struct, IEntityQueryCallback
        {
            public static readonly IntPtr jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(Callback), (ExecuteJobFunction)Execute);

            public delegate void ExecuteJobFunction(ref Callback data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref Callback jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                ContainerQueryParams* queryParams = (ContainerQueryParams*)additionalPtr;
                var container = queryParams->container;
                var containerID = container->ID;
                var entitySize = container->entitySize;
                while (true)
                {
                    int begin;
                    int end;
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                        break;
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
                    while (container->GetRange(ref begin, ref end,
                        out EntityChunk* chunk, out int chunkStartInclude,
                        out int innerStartInclude, out int innerEndExclude))
                    {
                        byte* chunkData = chunk->data;
                        int innerEnd = innerEndExclude;
                        int chunkStart = chunkStartInclude;
                        for (int i = innerStartInclude; i < innerEnd; i++)
                        {
                            var entityData = (EntityData*)(chunkData + i * entitySize);
                            Entity entity = new Entity(containerID, entityData->InnerKey, chunkStart + i);
                            QueryResult queryResult = new QueryResult(entityData, entity, queryParams);
                            jobData.Execute(ref queryResult);
                        }
                    }
                }
            }
        }
    }
}
