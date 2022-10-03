using E.Collections.Unsafe;
using System.Diagnostics;

namespace E.Entities
{
    public unsafe struct QueryResult
    {
        internal QueryResult(EntityData* entityData, Entity entity, ContainerQueryParams* queryParams)
        {
            m_EntityData = entityData;
            m_Entity = entity;
            m_QueryParams = queryParams;
        }

        private readonly EntityData* m_EntityData;

        private readonly Entity m_Entity;

        private readonly ContainerQueryParams* m_QueryParams;

        public Entity GetIdentity() => m_Entity;

        public SpinLock GetLock() => m_EntityData->GetLock();

        public Reference<T> GetComponent<T>(byte queryIndex)
            where T : unmanaged, IComponent
        {
            CheckComponentCount(queryIndex);
            short offset = m_QueryParams->offsets[queryIndex];
            CheckComponentExists<T>(offset, queryIndex);
            return new Reference<T>((T*)(m_EntityData->Value + offset));
        }

        #region Check

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckComponentCount(byte queryIndex)
        {
            if (queryIndex >= m_QueryParams->componentCount)
            {
                throw new System.IndexOutOfRangeException($"queryIndex must be less then {m_QueryParams->componentCount}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckComponentExists<T>(short offset, byte queryIndex)
            where T : unmanaged, IComponent
        {
            if (offset == -1 || (m_QueryParams->ids[queryIndex] != ComponentTypeIdentity<T>.GetID()))
            {
                throw new System.ArgumentException("ComponentType not match.", "queryIndex");
            }
        }

        #endregion
    }
}
