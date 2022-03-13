using E.Collections;
using Unity.Jobs;

namespace E.Entities
{
    public unsafe partial struct EntityScene
    {
        internal partial struct Instance
        {
            #region Query groups

            public void QueryEntities4<Callback>(ref Callback callback, ref QueryParams queryParams, in ComponentTypeGroup componentTypes)
                where Callback : struct, IEntityQueryCallback4
            {
                if (m_GroupCount == 0) return;
                int usedMaskLength = GetUsedMaskLength();
                ulong* usedMask = stackalloc ulong[usedMaskLength];
                InitUsedMask(usedMask, usedMaskLength, componentTypes);
                for (int usedMaskIndex = 0; usedMaskIndex < usedMaskLength; usedMaskIndex++)
                {
                    var currUsedMask = usedMask[usedMaskIndex];
                    while (currUsedMask != 0)
                    {
                        short offset = (short)BitUtility.GetTrailingZerosCount((long)currUsedMask);
                        currUsedMask = (ulong)BitUtility.RemoveLowestOne((long)currUsedMask);
                        short searchedID = (short)(64 * usedMaskIndex + offset + 1);
                        EntityContainer* container = GetConfirmedContainer(searchedID);
                        if (container->entityCount > 0)
                        {
                            InvokeCallback4(ref callback, ref queryParams, container);
                        }
                    }
                }
            }

            public void QueryEntities8<Callback>(ref Callback callback, ref QueryParams queryParams, in ComponentTypeGroup componentTypes)
                where Callback : struct, IEntityQueryCallback8
            {
                if (m_GroupCount == 0) return;
                int usedMaskLength = GetUsedMaskLength();
                ulong* usedMask = stackalloc ulong[usedMaskLength];
                InitUsedMask(usedMask, usedMaskLength, componentTypes);
                for (int usedMaskIndex = 0; usedMaskIndex < usedMaskLength; usedMaskIndex++)
                {
                    var currUsedMask = usedMask[usedMaskIndex];
                    while (currUsedMask != 0)
                    {
                        short offset = (short)BitUtility.GetTrailingZerosCount((long)currUsedMask);
                        currUsedMask = (ulong)BitUtility.RemoveLowestOne((long)currUsedMask);
                        short searchedID = (short)(64 * usedMaskIndex + offset + 1);
                        EntityContainer* container = GetConfirmedContainer(searchedID);
                        if (container->entityCount > 0)
                        {
                            InvokeCallback8(ref callback, ref queryParams, container);
                        }
                    }
                }
            }

            public void QueryGroups<Callback>(ref Callback callback, in ComponentTypeGroup componentTypes)
                where Callback : struct, IEntityGroupQueryCallback
            {
                if (m_GroupCount == 0) return;
                int usedMaskLength = GetUsedMaskLength();
                ulong* usedMask = stackalloc ulong[usedMaskLength];
                InitUsedMask(usedMask, usedMaskLength, componentTypes);
                for (int usedMaskIndex = 0; usedMaskIndex < usedMaskLength; usedMaskIndex++)
                {
                    var currUsedMask = usedMask[usedMaskIndex];
                    while (currUsedMask != 0)
                    {
                        short offset = (short)BitUtility.GetTrailingZerosCount((long)currUsedMask);
                        currUsedMask = (ulong)BitUtility.RemoveLowestOne((long)currUsedMask);
                        short searchedID = (short)(64 * usedMaskIndex + offset + 1);
                        EntityContainer* container = GetConfirmedContainer(searchedID);
                        EntityGroup entityGroup = new EntityGroup(container);
                        callback.Execute(entityGroup);
                    }
                }
            }

            private void InvokeCallback4<Callback>(ref Callback callback, ref QueryParams queryParams, EntityContainer* container)
                where Callback : struct, IEntityQueryCallback4
            {
                var containerQueryParams = GetContainerQueryParams(ref queryParams, container);
                var scheduleMode = queryParams.ScheduleMode;
                switch (scheduleMode)
                {
                    case ScheduleMode.Run:
                    case ScheduleMode.Single:
                        queryParams.target = callback.Schedule4(ref containerQueryParams, scheduleMode, queryParams.target);
                        break;
                    case ScheduleMode.Parallel:
                        var parallelHandle = callback.Schedule4(ref containerQueryParams, scheduleMode, queryParams.dependsOn);
                        queryParams.target = JobHandle.CombineDependencies(queryParams.target, parallelHandle);
                        break;
                }
            }

            private void InvokeCallback8<Callback>(ref Callback callback, ref QueryParams queryParams, EntityContainer* container)
                where Callback : struct, IEntityQueryCallback8
            {
                var containerQueryParams = GetContainerQueryParams(ref queryParams, container);
                var scheduleMode = queryParams.ScheduleMode;
                switch (scheduleMode)
                {
                    case ScheduleMode.Run:
                    case ScheduleMode.Single:
                        queryParams.target = callback.Schedule8(ref containerQueryParams, scheduleMode, queryParams.target);
                        break;
                    case ScheduleMode.Parallel:
                        var parallelHandle = callback.Schedule8(ref containerQueryParams, scheduleMode, queryParams.dependsOn);
                        queryParams.target = JobHandle.CombineDependencies(queryParams.target, parallelHandle);
                        break;
                }
            }

            private ContainerQueryParams GetContainerQueryParams(ref QueryParams queryParams, EntityContainer* container)
            {
                ContainerQueryParams containerQueryParams = new ContainerQueryParams()
                { container = container, length = container->entityCount };
                short* offsetsPtr = containerQueryParams.offsets;
                // set offsets to -1
                *(long*)offsetsPtr = -1;
                *(long*)(offsetsPtr + 4) = -1;
                int comCount = queryParams.Count;
                if (comCount > 0)
                {
                    byte* ids;
                    fixed (byte* idsPtr = queryParams.ids)
                    {
                        ids = idsPtr;
                    }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    // set ids
                    byte* cids = containerQueryParams.ids;
                    *(long*)cids = *(long*)ids;
#endif
                    // set offsets
                    var comsData = &container->componentsData;
                    for (int i = 0; i < comCount; i++)
                    {
                        var offset = comsData->OffsetOf(ids[i]);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        if (offset == -1)
                        {
                            throw new System.ArgumentException($"{ComponentTypeGlobal.ChunkPtr->GetComponentType(ids[i])} not exists in this Container.");
                        }
#endif
                        offsetsPtr[i] = (short)offset;
                    }
                }
                return containerQueryParams;
            }

            #endregion

            #region Match unique container ID

            public short MatchUniqueContainerID(in ComponentTypeGroup componentTypes)
            {
                short searchedID = TryMatchUniqueContainerID(componentTypes);
                if (searchedID == -1)
                {
                    using (GetLock())
                    {
                        searchedID = TryMatchUniqueContainerID(componentTypes);
                        if (searchedID != -1) return searchedID;
                        ulong* inUseMaskListPtr = null;
                        fixed (byte* ptr = m_GroupsInUseMaskList)
                        {
                            inUseMaskListPtr = (ulong*)ptr;
                        }
                        // New id.
                        short inUseIndex = m_GroupCount;
                        int guideIndex = (inUseIndex) >> 6;
                        int offset = inUseIndex & 0b111111;
                        ComponentTypeGroup.IDEnumerator idEnumerator = componentTypes.GetIDEnumerator();
                        while (idEnumerator.MoveNext())
                        {
                            var currInUseMask = inUseMaskListPtr + idEnumerator.Current * 8;
                            currInUseMask[guideIndex] |= (1ul << offset);
                        }
                        m_GroupComCounts[inUseIndex] = (byte)componentTypes.Count;
                        searchedID = (short)(inUseIndex + 1);
                        m_GroupCount = searchedID;
                    }
                }
                return searchedID;
            }

            public short TryMatchUniqueContainerID(in ComponentTypeGroup componentTypes)
            {
                if (m_GroupCount == 0) return -1;
                fixed (byte* groupComCountsPtr = m_GroupComCounts)
                {
                    MatchUniqueContainerIDCallback callback = new MatchUniqueContainerIDCallback(componentTypes.Count, groupComCountsPtr);
                    return TryMatchContainerIDs(ref callback, componentTypes);
                }
            }

            private interface IMatchContainerIDsCallback
            {
                bool Invoke(short searchedID);
            }

            private struct MatchUniqueContainerIDCallback : IMatchContainerIDsCallback
            {
                public MatchUniqueContainerIDCallback(int targetComCount, byte* groupComCounts)
                {
                    m_TargetComCount = targetComCount;
                    m_GroupComCounts = groupComCounts;
                }

                private readonly int m_TargetComCount;

                private readonly byte* m_GroupComCounts;

                public bool Invoke(short searchedID)
                    => m_TargetComCount == m_GroupComCounts[searchedID - 1];
            }

            private short TryMatchContainerIDs<Callback>(ref Callback callback, in ComponentTypeGroup componentTypes)
                where Callback : struct, IMatchContainerIDsCallback
            {
                int usedMaskLength = GetUsedMaskLength();
                ulong* usedMask = stackalloc ulong[usedMaskLength];
                InitUsedMask(usedMask, usedMaskLength, componentTypes);
                return GetSearchedID(usedMask, usedMaskLength, ref callback);
            }

            private int GetUsedMaskLength()
            {
                short tempCount = m_GroupCount;
                int usedMaskLength = (tempCount) >> 6;
                int usedMaskLengthRem = tempCount & 0b111111;
                usedMaskLength = usedMaskLengthRem == 0 ? usedMaskLength : usedMaskLength + 1;
                return usedMaskLength;
            }

            private void InitUsedMask(ulong* usedMask, int usedMaskLength, in ComponentTypeGroup componentTypes)
            {
                int usedMaskIndex = 0;
                for (usedMaskIndex = 0; usedMaskIndex < usedMaskLength; usedMaskIndex++)
                {
                    usedMask[usedMaskIndex] = ulong.MaxValue;
                }
                // &
                ulong* inUseMaskListPtr = null;
                fixed (byte* ptr = m_GroupsInUseMaskList)
                {
                    inUseMaskListPtr = (ulong*)ptr;
                }
                ComponentTypeGroup.IDEnumerator idEnumerator = componentTypes.GetIDEnumerator();
                while (idEnumerator.MoveNext())
                {
                    var currInUseMask = inUseMaskListPtr + idEnumerator.Current * 8;
                    for (usedMaskIndex = 0; usedMaskIndex < usedMaskLength; usedMaskIndex++)
                    {
                        usedMask[usedMaskIndex] &= currInUseMask[usedMaskIndex];
                    }
                }
            }

            private short GetSearchedID<Callback>(ulong* usedMask, int usedMaskLength, ref Callback callback)
                where Callback : struct, IMatchContainerIDsCallback
            {
                for (int usedMaskIndex = 0; usedMaskIndex < usedMaskLength; usedMaskIndex++)
                {
                    var currUsedMask = usedMask[usedMaskIndex];
                    while (currUsedMask != 0)
                    {
                        short offset = (short)BitUtility.GetTrailingZerosCount((long)currUsedMask);
                        currUsedMask = (ulong)BitUtility.RemoveLowestOne((long)currUsedMask);
                        short searchedID = (short)(64 * usedMaskIndex + offset + 1);
                        if (callback.Invoke(searchedID)) return searchedID;
                    }
                }
                return -1;
            }

            #endregion
        }
    }
}