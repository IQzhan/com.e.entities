using E.Collections.Unsafe;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace E.Entities
{
    public unsafe struct EntityComponentsData
    {
        #region Main

        [NativeDisableUnsafePtrRestriction]
        private readonly EntityContainer* m_Container;

        [NativeDisableUnsafePtrRestriction]
        private readonly EntityData* m_EntityData;

        /// <summary>
        /// if singleton, this as offset of component for check,
        /// else as index of entity.
        /// </summary>
        private readonly int m_Position;

        public bool IsCreated => m_Container != null && m_Container->IsCreated;

        internal EntityComponentsData(
            EntityContainer* container,
            EntityData* entityData,
            int position)
        {
            m_Container = container;
            m_EntityData = entityData;
            m_Position = position;
        }

        public bool IsInvalid
        {
            get
            {
                CheckExists();
                return m_EntityData->IsInvalid;
            }
        }

        public Entity GetIdentity()
        {
            CheckExists();
            return new Entity(m_Container->ID, m_EntityData->InnerKey, m_Position);
        }

        public SpinLock GetLock()
        {
            CheckExists();
            return m_EntityData->GetLock();
        }

        public bool HasComponent<T>()
            where T : unmanaged, IComponentStructure
        {
            CheckExists();
            int offset = m_Container->componentsData.OffsetOf<T>();
            if (m_Container->ID == 0)
            {
                return m_Position == offset;
            }
            return offset != -1;
        }

        public bool HasComponent(ComponentType componentType)
        {
            CheckExists();
            int offset = m_Container->componentsData.OffsetOf(componentType);
            if (m_Container->ID == 0)
            {
                return m_Position == offset;
            }
            return offset != -1;
        }

        public Reference<T> GetComponent<T>()
            where T : unmanaged, IComponentStructure
        {
            CheckExists();
            int offset = m_Container->componentsData.OffsetOf<T>();
            CheckOffset<T>(offset);
            if (m_Container->ID == 0)
            {
                // is singleton
                CheckSingletonOffset(offset);
                return new Reference<T>((T*)(m_EntityData->Value));
            }
            else
            {
                return new Reference<T>((T*)(m_EntityData->Value + offset));
            }
        }

        public IntPtr GetComponent(ComponentType componentType)
        {
            CheckExists();
            int offset = m_Container->componentsData.OffsetOf(componentType);
            CheckOffset(componentType, offset);
            if (m_Container->ID == 0)
            {
                // is singleton
                CheckSingletonOffset(offset);
                return (IntPtr)(m_EntityData->Value);
            }
            else
            {
                return (IntPtr)(m_EntityData->Value + offset);
            }
        }

        public IntPtr GetComponentsPtr()
        {
            CheckExists();
            return (IntPtr)(m_EntityData->Value);
        }

        #endregion

        #region Check

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new NullReferenceException($"{nameof(EntityComponentsData)} is yet created or already disposed.");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private void CheckOffset<T>(int offset)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (offset == -1)
            {
                throw new ArgumentException($"There is no component {typeof(T)} in Group {m_Container->ID}");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private void CheckOffset(ComponentType componentType, int offset)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (offset == -1)
            {
                throw new ArgumentException($"There is no component {componentType.SystemType} in Group {m_Container->ID}");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private void CheckSingletonOffset(int offset)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_Position != offset)
            {
                throw new ArgumentException($"Offset not match this {nameof(EntityComponentsData)}");
            }
#endif
        }

        #endregion
    }
}