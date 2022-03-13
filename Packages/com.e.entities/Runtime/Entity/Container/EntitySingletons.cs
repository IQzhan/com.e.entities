using E.Collections.Unsafe;
using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace E.Entities
{
    public unsafe struct EntitySingletons : IEquatable<EntitySingletons>
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly EntityContainer* m_Body;

        internal EntitySingletons(EntityContainer* body)
        {
            m_Body = body;
        }

        public bool IsCreated
        {
            get
            {
                return (m_Body != null) && m_Body->IsCreated;
            }
        }

        /// <summary>
        /// Get ComponentTypeGroup
        /// </summary>
        public ComponentTypeGroup ComponentGroup
        {
            get
            {
                CheckExists();
                return m_Body->componentsData.GetComponentGroup();
            }
        }

        /// <summary>
        /// Component count.
        /// </summary>
        public int ComponentCount
        {
            get
            {
                CheckExists();
                return m_Body->componentsData.ComponentCount;
            }
        }

        /// <summary>
        /// Total size.
        /// </summary>
        public int DataSize
        {
            get
            {
                CheckExists();
                return m_Body->componentsData.DataSize;
            }
        }

        public bool HasComponent<T>()
            where T : unmanaged, IComponentStructure
        {
            CheckExists();
            int offset = m_Body->componentsData.OffsetOf<T>();
            return offset != -1;
        }

        public bool HasComponent(ComponentType componentType)
        {
            CheckExists();
            int offset = m_Body->componentsData.OffsetOf(componentType);
            return offset != -1;
        }

        public EntityComponentsData GetSingleton(ComponentType componentType)
        {
            CheckExists();
            return m_Body->GetSingleton(componentType);
        }

        public override bool Equals(object obj)
            => obj is EntitySingletons group && (m_Body == group.m_Body);

        public bool Equals(EntitySingletons other)
            => m_Body == other.m_Body;

        public override int GetHashCode()
            => HashCode.Combine((long)m_Body);

        public static bool operator ==(EntitySingletons left, EntitySingletons right)
            => left.m_Body == right.m_Body;

        public static bool operator !=(EntitySingletons left, EntitySingletons right)
            => left.m_Body != right.m_Body;

        public override string ToString()
            => IsCreated ? $"[EntitySingletons] ComponentCount: {ComponentCount}" : "[EntitySingletons] null";

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new NullReferenceException($"{nameof(EntitySingletons)} is yet created or already disposed.");
            }
#endif
        }
    }

    #region Debug

    public sealed unsafe class EntitySingletonsDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly EntitySingletons m_Instance;

        public EntitySingletonsDebugView(EntitySingletons instance)
        {
            m_Instance = instance;
        }

        public int ComponentCount => m_Instance.IsCreated ? m_Instance.ComponentCount : 0;

        public int DataSize => m_Instance.IsCreated ? m_Instance.DataSize : 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public object[] Singletons
        {
            get
            {
                if (!m_Instance.IsCreated) return new object[0];
                int count = ComponentCount;
                var componentTypes = m_Instance.ComponentGroup;
                var container = m_Instance.m_Body;
                var componentsData = &container->componentsData;
                object[] singletons = new object[count];
                int comIndex = 0;
                foreach (var componentType in componentTypes)
                {
                    int offset = componentsData->OffsetOf(componentType);
                    var entityData = container->InternalGet(offset);
                    // convert to object
                    object obj = Memory.PtrToStructure(entityData->Value, componentType.SystemType);
                    singletons[comIndex++] = obj;
                }
                return singletons;
            }
        }
    }

    #endregion
}