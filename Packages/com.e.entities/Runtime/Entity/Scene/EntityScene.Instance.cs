using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Memory = E.Collections.Unsafe.Memory;
using SpinLock = E.Collections.Unsafe.SpinLock;

namespace E.Entities
{
    public unsafe partial struct EntityScene : IEquatable<EntityScene>
    {
        internal partial struct Instance
        {
            #region Main

            private int m_SpinLock;

            /// <summary>
            /// Unique id.
            /// </summary>
            private short m_ID;

            /// <summary>
            /// Group count.
            /// </summary>
            private short m_GroupCount;

            /// <summary>
            /// EntityContainer*
            /// </summary>
            private fixed long m_Containers[EntityConst.MaxEntityGroupCountEachScene + 1];

            /// <summary>
            /// Group component count for search.
            /// </summary>
            private fixed byte m_GroupComCounts[EntityConst.MaxEntityGroupCountEachScene];

            /// <summary>
            /// Divided into 256 parts(256 ComponentTypes), and the size of each part is 64 bytes.
            /// The position of each bit corresponds to the ID of a group.
            /// </summary>
            private fixed byte m_GroupsInUseMaskList[EntityConst.ChunkSize];

            public short ID { get => (short)(m_ID - 1); set => m_ID = (short)(value + 1); }

            public bool IsCreated => m_ID > 0;

            public short GroupCount => m_GroupCount;

            public void Initialize(short id)
            {
                this = default;
                ID = id;
            }

            public void Dispose()
            {
                // Dispose all containers.
                for (short i = 0; i <= m_GroupCount; i++)
                {
                    DisposeContainer(i);
                }
                this = default;
            }

            public bool Complete(out JobHandle result)
            {
                result = default;
                if (m_GroupCount == 0)
                {
                    return false;
                }
                bool returnResult = false;
                for (short i = 1; i <= m_GroupCount; i++)
                {
                    var container = GetConfirmedContainer(i);
                    container->CompleteCreateEntities();
                    int moveCount = container->GetRemovingAndRemovedCount();
                    if (moveCount < 32)
                    {
                        container->CompleteRemoveEntities();
                    }
                    else
                    {
                        CompleteRemoveEntitiesJob removeJob = new CompleteRemoveEntitiesJob(container);
                        JobHandle handle = removeJob.Schedule();
                        result = JobHandle.CombineDependencies(result, handle);
                        returnResult = true;
                    }
                }
                return returnResult;
            }

            [BurstCompile]
            private struct CompleteRemoveEntitiesJob : IJob
            {
                private readonly EntityContainer* m_Container;

                public CompleteRemoveEntitiesJob(EntityContainer* container)
                {
                    m_Container = container;
                }

                public void Execute()
                {
                    m_Container->CompleteRemoveEntities();
                }
            }

            /// <summary>
            /// Get or create a singleon container.
            /// </summary>
            /// <returns></returns>
            public EntitySingletons GetSingletons()
            {
                var container = GetOrCreateContainer(0, ComponentTypeGroup.Null);
                return new EntitySingletons(container);
            }

            /// <summary>
            /// Get or create an entity group.
            /// </summary>
            /// <param name="componentTypes"></param>
            /// <returns></returns>
            public EntityGroup GetGroup(in ComponentTypeGroup componentTypes)
            {
                CheckGroup(componentTypes);
                short index = MatchUniqueContainerID(componentTypes);
                var container = GetOrCreateContainer(index, componentTypes);
                return new EntityGroup(container);
            }

            public EntityGroup GetGroup(int id)
            {
                CheckGroup(id);
                var container = GetContainer((short)id);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (container == null)
                {
                    throw new ArgumentException($"EntityGroup id: {id} not create yet.");
                }
#endif
                return new EntityGroup(container);
            }

            /// <summary>
            /// Create a query.
            /// </summary>
            /// <param name="componentTypes"></param>
            /// <returns></returns>
            public EntityQuery Query(in ComponentTypeGroup componentTypes)
            {
                CheckGroup(componentTypes);
                fixed (Instance* ptr = &this)
                {
                    return new EntityQuery(ptr, componentTypes);
                }
            }

            #endregion

            #region Internal

            private EntityContainer* GetOrCreateContainer(short index, in ComponentTypeGroup componentTypes)
            {
                var container = GetContainer(index);
                if (container == null)
                {
                    using (GetLock())
                    {
                        container = GetContainer(index);
                        if (container != null) return container;
                        container = RequireEntityContainer();
                        container->Initialize(index, componentTypes);
                        SetGetContainer(index, container);
                    }
                }
                return container;
            }

            private void DisposeContainer(short index)
            {
                var container = GetContainer(index);
                if (container == null) return;
                container->Dispose();
                ReleaseEntityContainer(ref container);
                SetGetContainer(index, container);
            }

            private EntityContainer* GetConfirmedContainer(short index)
            {
                var container = GetContainer(index);
                if (container == null)
                {
                    using (GetLock())
                    {
                        while ((container = GetContainer(index)) == null) { }
                    }
                }
                return container;
            }

            private EntityContainer* RequireEntityContainer()
            {
                return (EntityContainer*)Memory.Malloc<EntityContainer>(1, Allocator.Persistent);
            }

            private void ReleaseEntityContainer(ref EntityContainer* container)
            {
                Memory.Free(container, Allocator.Persistent);
                container = null;
            }

            private EntityContainer* GetContainer(short index)
            {
                fixed (long* ptr = m_Containers)
                {
                    return *(((EntityContainer**)ptr) + index);
                }
            }

            private void SetGetContainer(short index, EntityContainer* container)
            {
                fixed (long* ptr = m_Containers)
                {
                    *(((EntityContainer**)ptr) + index) = container;
                }
            }

            private SpinLock GetLock()
            {
                fixed (Instance* ptr = &this)
                {
                    return new SpinLock(&ptr->m_SpinLock);
                }
            }

            #endregion

            #region Check

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckGroup(in ComponentTypeGroup componentTypes)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (componentTypes == ComponentTypeGroup.Null)
                {
                    throw new ArgumentNullException("componentTypes");
                }
                if (componentTypes.Count > 32)
                {
                    throw new ArgumentException("Too many types", "componentTypes");
                }
                if(componentTypes.Mode == ComponentMode.Singleton)
                {
                    throw new ArgumentException("Can not be singletons.", "componentTypes");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckGroup(int id)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (id < 1 || id > EntityConst.MaxEntityGroupCountEachScene)
                {
                    throw new IndexOutOfRangeException($"id must in [1, {EntityConst.MaxEntityGroupCountEachScene}]");
                }
#endif
            }

            #endregion
        }

        [NativeDisableUnsafePtrRestriction]
        private readonly Instance* m_Body;

        private EntityScene(Instance* body)
        {
            m_Body = body;
        }

        public bool IsCreated => (m_Body != null) && m_Body->IsCreated;

        public int ID
        {
            get
            {
                CheckExists();
                return m_Body->ID;
            }
        }

        public EntitySingletons GetSingletons()
        {
            CheckExists();
            return m_Body->GetSingletons();
        }

        public EntityGroup GetGroup(ComponentTypeGroup componentTypes)
        {
            CheckExists();
            return m_Body->GetGroup(componentTypes);
        }

        public EntityQuery Query(ComponentTypeGroup componentTypes)
        {
            CheckExists();
            return m_Body->Query(componentTypes);
        }

        public override bool Equals(object obj)
            => obj is EntityScene scene && Equals(scene);

        public bool Equals(EntityScene other)
            => m_Body == other.m_Body;

        public override int GetHashCode()
            => HashCode.Combine((long)m_Body);

        public static bool operator ==(EntityScene left, EntityScene right)
            => left.m_Body == right.m_Body;

        public static bool operator !=(EntityScene left, EntityScene right)
            => left.m_Body != right.m_Body;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new NullReferenceException($"{nameof(EntityScene)} is yet created or already disposed.");
            }
#endif
        }
    }
}