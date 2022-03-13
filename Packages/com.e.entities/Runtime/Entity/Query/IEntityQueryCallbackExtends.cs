using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace E.Entities
{
    internal unsafe static class IEntityQueryCallbackExtends
    {
        public static JobHandle Schedule4<T>(ref this T jobData, ref ContainerQueryParams containerQueryParams, ScheduleMode scheduleMode, JobHandle dependsOn = new JobHandle())
            where T : struct, IEntityQueryCallback4
        {
            if (scheduleMode == ScheduleMode.Run)
            {
                dependsOn.Complete();
                jobData.Execute4(ref containerQueryParams);
                return dependsOn;
            }
            int innerloopBatchCount = (scheduleMode == ScheduleMode.Single) ? containerQueryParams.length : 64;
            void* listData = QueryJobAdditionalDataMemory.Require();
            *(ContainerQueryParams*)listData = containerQueryParams;
            var scheduleParams = new JobsUtility.JobScheduleParameters
                (UnsafeUtility.AddressOf(ref jobData), JobStruct4<T>.jobReflectionData, dependsOn, (Unity.Jobs.LowLevel.Unsafe.ScheduleMode)scheduleMode);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, listData, null);
        }

        public static JobHandle Schedule8<T>(ref this T jobData, ref ContainerQueryParams containerQueryParams, ScheduleMode scheduleMode, JobHandle dependsOn = new JobHandle())
            where T : struct, IEntityQueryCallback8
        {
            if (scheduleMode == ScheduleMode.Run)
            {
                dependsOn.Complete();
                jobData.Execute8(ref containerQueryParams);
                return dependsOn;
            }
            int innerloopBatchCount = (scheduleMode == ScheduleMode.Single) ? containerQueryParams.length : 64;
            void* listData = QueryJobAdditionalDataMemory.Require();
            *(ContainerQueryParams*)listData = containerQueryParams;
            var scheduleParams = new JobsUtility.JobScheduleParameters
                (UnsafeUtility.AddressOf(ref jobData), JobStruct8<T>.jobReflectionData, dependsOn, (Unity.Jobs.LowLevel.Unsafe.ScheduleMode)scheduleMode);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, listData, null);
        }

        private static void Execute4<T>(ref this T jobData, ref ContainerQueryParams containerQueryParams)
            where T : struct, IEntityQueryCallback4
        {
            var container = containerQueryParams.container;
            var containerID = container->ID;
            var entitySize = container->entitySize;
            long offsets0;
            fixed (short* offsetsPtr = containerQueryParams.offsets)
            {
                offsets0 = *(long*)offsetsPtr;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            long ids;
            fixed (byte* idsPtr = containerQueryParams.ids)
            {
                ids = *(long*)idsPtr;
            }
#endif
            int begin = 0;
            int end = containerQueryParams.length;
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
                    QueryResult4 queryResult = new QueryResult4(entityData, entity);
                    queryResult.SetOffsets(offsets0);
                    queryResult.SetIDs(ids);
                    jobData.Execute(ref queryResult);
                }
            }
        }

        public class JobStruct4<Callback>
            where Callback : struct, IEntityQueryCallback4
        {
            public static readonly IntPtr jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(Callback), (ExecuteJobFunction)Execute);

            public delegate void ExecuteJobFunction(ref Callback data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref Callback jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                ContainerQueryParams containerQueryParams = *(ContainerQueryParams*)additionalPtr;
                var container = containerQueryParams.container;
                var containerID = container->ID;
                var entitySize = container->entitySize;
                var offsets0 = *(long*)containerQueryParams.offsets;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var ids = *(long*)containerQueryParams.ids;
#endif
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
                            QueryResult4 queryResult = new QueryResult4(entityData, entity);
                            queryResult.SetOffsets(offsets0);
                            queryResult.SetIDs(ids);
                            jobData.Execute(ref queryResult);
                        }
                    }
                }
            }
        }

        private static void Execute8<T>(ref this T jobData, ref ContainerQueryParams containerQueryParams)
            where T : struct, IEntityQueryCallback8
        {
            var container = containerQueryParams.container;
            var containerID = container->ID;
            var entitySize = container->entitySize;
            long offsets0;
            long offsets1;
            fixed (short* offsetsPtr = containerQueryParams.offsets)
            {
                offsets0 = *(long*)offsetsPtr;
                offsets1 = *(long*)(offsetsPtr + 4);
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            long ids;
            fixed (byte* idsPtr = containerQueryParams.ids)
            {
                ids = *(long*)idsPtr;
            }
#endif
            int begin = 0;
            int end = containerQueryParams.length;
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
                    QueryResult8 queryResult = new QueryResult8(entityData, entity);
                    queryResult.SetOffsets(offsets0, offsets1);
                    queryResult.SetIDs(ids);
                    jobData.Execute(ref queryResult);
                }
            }
        }

        public class JobStruct8<Callback>
            where Callback : struct, IEntityQueryCallback8
        {
            public static readonly IntPtr jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(Callback), (ExecuteJobFunction)Execute);

            public delegate void ExecuteJobFunction(ref Callback data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref Callback jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                ContainerQueryParams containerQueryParams = *(ContainerQueryParams*)additionalPtr;
                var container = containerQueryParams.container;
                var containerID = container->ID;
                var entitySize = container->entitySize;
                var offsets0 = *(long*)containerQueryParams.offsets;
                var offsets1 = *(long*)(containerQueryParams.offsets + 4);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var ids = *(long*)containerQueryParams.ids;
#endif
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
                            QueryResult8 queryResult = new QueryResult8(entityData, entity);
                            queryResult.SetOffsets(offsets0, offsets1);
                            queryResult.SetIDs(ids);
                            jobData.Execute(ref queryResult);
                        }
                    }
                }
            }
        }
    }
}