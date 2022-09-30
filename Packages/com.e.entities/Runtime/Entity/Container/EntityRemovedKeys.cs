using E.Collections.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;

namespace E.Entities
{
    [DebuggerTypeProxy(typeof(RemovedEntitiesDebugView))]
    public struct RemovedEntities : IEnumerable<Entity>, IEnumerable
    {
        internal readonly short m_ContainerID;

        internal readonly UnsafeBitMask m_Keys;

        public bool IsCreated => m_Keys.IsCreated;

        internal RemovedEntities(short containerID, EntityRemovedKeys keys)
        {
            m_ContainerID = containerID;
            m_Keys = keys.m_Mask;
        }

        public Enumerator GetEnumerator() => new Enumerator(m_ContainerID, m_Keys);

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => new Enumerator(m_ContainerID, m_Keys);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(m_ContainerID, m_Keys);

        public struct Enumerator : IEnumerator<Entity>, IEnumerator, IDisposable
        {
            private readonly short m_ContainerID;

            private readonly UnsafeBitMask m_Keys;

            private int m_Index;

            object IEnumerator.Current => GetCurrent();

            public Entity Current => GetCurrent();

            internal Enumerator(short containerID, UnsafeBitMask keys)
            {
                m_ContainerID = containerID;
                m_Keys = keys;
                m_Index = -1;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public bool MoveNext()
            {
                long nextIndex = m_Index + 1;
                if (nextIndex < m_Keys.Capacity)
                {
                    nextIndex = m_Keys.GetFirst(nextIndex);
                    if (nextIndex != -1)
                    {
                        m_Index = (int)nextIndex;
                        return true;
                    }
                    else
                    {
                        m_Index = (int)m_Keys.Capacity;
                    }
                }
                return false;
            }

            private Entity GetCurrent()
            {
                return new Entity(m_ContainerID, m_Index, -1);
            }

            public void Dispose()
            {

            }
        }
    }

    internal struct EntityRemovedKeys
    {
        internal UnsafeBitMask m_Mask;

        public int Count => m_Mask.Count;

        public void Initialize()
        {
            m_Mask = new UnsafeBitMask(EntityConst.BitMaskExpandSize, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (!m_Mask.IsCreated) return;
            m_Mask.Dispose();
            m_Mask = default;
        }

        public void SetMaxCount(int maxCount)
        {
            if (maxCount > m_Mask.Capacity)
            {
                using (m_Mask.GetLock())
                {
                    if (maxCount <= m_Mask.Capacity) return;
                    int expsize = maxCount - (int)m_Mask.Capacity;
                    int rem = expsize % EntityConst.BitMaskExpandSize;
                    expsize = rem == 0 ? expsize : (expsize + (EntityConst.BitMaskExpandSize - rem));
                    m_Mask.Expand(expsize);
                }
            }
        }

        public void Insert(int key)
        {
            m_Mask.Set(key, true);
        }

        public int GetFirstThenRemove()
        {
            return (int)m_Mask.GetFirstThenRemove();
        }
    }

    #region Debug

    public sealed class RemovedEntitiesDebugView
    {

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RemovedEntities m_Instance;

        public RemovedEntitiesDebugView(RemovedEntities instance)
        {
            m_Instance = instance;
        }

        public int Count => m_Instance.IsCreated ? m_Instance.m_Keys.Count : 0;

        public Entity[] All
        {
            get
            {
                if (!m_Instance.IsCreated) return new Entity[0];
                int count = Count;
                Entity[] entities = new Entity[count];
                int index = 0;
                foreach (var entity in m_Instance)
                {
                    entities[index++] = entity;
                }
                return entities;
            }
        }
    }

    #endregion
}