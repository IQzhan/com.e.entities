using E.Collections.Unsafe;
using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace E.Entities
{
    /// <summary>
    /// Entities
    /// </summary>
    [DebuggerTypeProxy(typeof(EntityGroupDebugView))]
    public unsafe struct EntityGroup : IEquatable<EntityGroup>
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly EntityContainer* m_Body;

        internal EntityGroup(EntityContainer* body)
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
        /// Unique id in scene.
        /// </summary>
        public int ID
        {
            get
            {
                CheckExists();
                return m_Body->ID;
            }
        }

        /// <summary>
        /// How many entities.
        /// </summary>
        public int EntityCount
        {
            get
            {
                CheckExists();
                return m_Body->entityCount;
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
        /// Components count each entity.
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
        /// Components size each entity.
        /// </summary>
        public int ComponentsSize
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

        /// <summary>
        /// How many entities to create.
        /// </summary>
        /// <param name="count"></param>
        public void WillCreate(int count)
        {
            CheckExists();
            m_Body->WillCreate(count);
        }

        /// <summary>
        /// Create entity, must call WillCreate(int count) before create.
        /// </summary>
        /// <returns></returns>
        public EntityComponentsData Create()
        {
            CheckExists();
            return m_Body->CreateEntity();
        }

        /// <summary>
        /// Remove entity by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EntityComponentsData Remove(int index)
        {
            CheckExists();
            return m_Body->RemoveEntity(index);
        }

        /// <summary>
        /// Get entity by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EntityComponentsData Get(int index)
        {
            CheckExists();
            return m_Body->GetEntity(index);
        }

        /// <summary>
        /// Get removed entities in the previous frame.
        /// </summary>
        /// <returns></returns>
        public RemovedEntities GetRemovedEntities()
        {
            CheckExists();
            return m_Body->GetRemovedEntities();
        }

        public override bool Equals(object obj)
            => obj is EntityGroup group && (m_Body == group.m_Body);

        public bool Equals(EntityGroup other)
            => m_Body == other.m_Body;

        public override int GetHashCode()
            => HashCode.Combine((long)m_Body);

        public static bool operator ==(EntityGroup left, EntityGroup right)
            => left.m_Body == right.m_Body;

        public static bool operator !=(EntityGroup left, EntityGroup right)
            => left.m_Body != right.m_Body;

        public override string ToString()
            => IsCreated ? $"[EntityGrouop] ID: {ID}, EntityCount: {EntityCount}{System.Environment.NewLine}{ComponentGroup}" : "[EntityGrouop] null";

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new NullReferenceException($"{nameof(EntityGroup)} is yet created or already disposed.");
            }
#endif
        }
    }

    #region Debug

    public sealed unsafe class EntityGroupDebugView
    {
        public struct EntityView
        {
            public Entity entity;

            public object[] components;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly EntityGroup m_Instance;

        public EntityGroupDebugView(EntityGroup instance)
        {
            m_Instance = instance;
        }

        public int ID => m_Instance.IsCreated ? m_Instance.ID : -1;

        public int EntityCount => m_Instance.IsCreated ? m_Instance.EntityCount : 0;

        public ComponentTypeGroup ComponentGroup => m_Instance.IsCreated ? m_Instance.ComponentGroup : ComponentTypeGroup.Null;

        public int ComponentCount => m_Instance.IsCreated ? m_Instance.ComponentCount : 0;

        public int ComponentsSize => m_Instance.IsCreated ? m_Instance.ComponentsSize : 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public EntityView[] ExistingEntities
        {
            get
            {
                int count = m_Instance.IsCreated ? m_Instance.EntityCount : 0;
                EntityView[] data = new EntityView[count];
                if (count == 0) return data;
                var container = m_Instance.m_Body;
                var group = m_Instance.ComponentGroup;
                var chunkList = container->chunkList;
                for (int i = 0; i < data.Length; i++)
                {
                    EntityData* entityData = container->InternalGet(chunkList, i);
                    var comData = container->NewEntityComponentsData(entityData, i);
                    object[] components = new object[group.Count];
                    int comIndex = 0;
                    foreach (var comType in group)
                    {
                        var comPtr = comData.GetComponent(comType);
                        // convert to object
                        object obj = Memory.PtrToStructure(comPtr, comType.SystemType);
                        components[comIndex++] = obj;
                    }
                    data[i] = new EntityView()
                    {
                        components = components,
                        entity = comData.GetIdentity()
                    };
                }
                return data;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public EntityView[] CreatingEntities
        {
            get
            {
                if (!m_Instance.IsCreated) return new EntityView[0];
                var group = m_Instance.ComponentGroup;
                var container = m_Instance.m_Body;
                var entityCount = container->entityCount;
                var appendEntityCountMid = container->appendEntityCountMid;
                var appendEntityCount = container->appendEntityCount;
                int count = appendEntityCountMid + appendEntityCount;
                EntityView[] data = new EntityView[count];
                int dataIndex = 0;
                var chunkList = container->chunkList;
                for (int i = entityCount; i < entityCount + appendEntityCountMid; i++)
                {
                    EntityData* entityData = container->InternalGet(chunkList, i);
                    var comData = container->NewEntityComponentsData(entityData, i);
                    int comIndex = 0;
                    object[] components = new object[group.Count];
                    foreach (var comType in group)
                    {
                        var comPtr = comData.GetComponent(comType);
                        // convert to object
                        object obj = Memory.PtrToStructure(comPtr, comType.SystemType);
                        components[comIndex++] = obj;
                    }
                    data[dataIndex++] = new EntityView()
                    {
                        components = components,
                        entity = comData.GetIdentity()
                    };
                }
                var appendChunkList = container->appendChunkList;
                for (int i = 0; i < appendEntityCount; i++)
                {
                    EntityData* entityData = container->InternalGet(appendChunkList, i);
                    var comData = container->NewEntityComponentsData(entityData, i);
                    int comIndex = 0;
                    object[] components = new object[group.Count];
                    foreach (var comType in group)
                    {
                        var comPtr = comData.GetComponent(comType);
                        // convert to object
                        object obj = Memory.PtrToStructure(comPtr, comType.SystemType);
                        components[comIndex++] = obj;
                    }
                    data[dataIndex++] = new EntityView()
                    {
                        components = components,
                        entity = comData.GetIdentity()
                    };
                }
                return data;
            }
        }

        public RemovedEntities RemovedEntities
        {
            get
            {
                return m_Instance.IsCreated ? m_Instance.GetRemovedEntities() : default;
            }
        }
    }

    #endregion
}