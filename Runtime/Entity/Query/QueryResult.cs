using E.Collections;
using E.Collections.Unsafe;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace E.Entities
{
    public unsafe struct QueryResult4
    {
        internal QueryResult4(EntityData* entityData, Entity entity)
        {
            m_EntityData = entityData;
            m_Entity = entity;
        }

        [NativeDisableUnsafePtrRestriction]
        private readonly EntityData* m_EntityData;

        private readonly Entity m_Entity;

        private fixed short m_Offsets[4];

        internal void SetOffsets(long offsets0)
        {
            fixed (short* offsetsPtr = m_Offsets)
            {
                *(long*)offsetsPtr = offsets0;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private fixed byte m_IDs[8];

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void SetIDs(long ids)
        {
            fixed (byte* idsPtr = m_IDs)
            {
                *(long*)idsPtr = ids;
            }
        }
#endif

        public Entity GetIdentity() => m_Entity;

        public SpinLock GetLock() => m_EntityData->GetLock();

        public Reference<T> GetComponent<T>(byte queryIndex)
            where T : unmanaged, IComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (queryIndex >= 4)
            {
                throw new System.IndexOutOfRangeException("queryIndex must be < 4.");
            }
#endif
            short offset = m_Offsets[queryIndex];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (offset == -1)
            {
                throw new System.ArgumentException("ComponentType not match..", "queryIndex");
            }
#endif
            return new Reference<T>((T*)(m_EntityData->Value + offset));
        }
    }

    public unsafe struct QueryResult8
    {
        internal QueryResult8(EntityData* entityData, Entity entity)
        {
            m_EntityData = entityData;
            m_Entity = entity;
        }

        [NativeDisableUnsafePtrRestriction]
        private readonly EntityData* m_EntityData;

        private readonly Entity m_Entity;

        private fixed short m_Offsets[8];

        internal void SetOffsets(long offsets0, long offsets1)
        {
            fixed (short* offsetsPtr = m_Offsets)
            {
                *(long*)offsetsPtr = offsets0;
                *(long*)(offsetsPtr + 4) = offsets1;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private fixed byte m_IDs[8];

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void SetIDs(long ids)
        {
            fixed (byte* idsPtr = m_IDs)
            {
                *(long*)idsPtr = ids;
            }
        }
#endif

        public Entity GetIdentity() => m_Entity;

        public SpinLock GetLock() => m_EntityData->GetLock();

        public Reference<T> GetComponent<T>(byte queryIndex)
            where T : unmanaged, IComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (queryIndex >= 8)
            {
                throw new System.IndexOutOfRangeException("queryIndex must < 8.");
            }
#endif
            short offset = m_Offsets[queryIndex];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (offset == -1 || (m_IDs[queryIndex] != ComponentTypeIdentity<T>.GetID()))
            {
                throw new System.ArgumentException("ComponentType not match.", "queryIndex");
            }
#endif
            return new Reference<T>((T*)(m_EntityData->Value + offset));
        }
    }
}