using E.Collections;
using E.Collections.Unsafe;
using Unity.Collections;

namespace E.Entities
{
    internal unsafe struct EntityChunkPool
    {
        private const int MaxCount = 25;

        private struct Instance
        {
            private int m_SpinLock;

            private UnsafeChunkedQueue m_Queue;

            private UnsafeChunkedQueue GetPool()
            {
                UnsafeChunkedQueue pool = m_Queue;
                if (!pool.IsCreated)
                {
                    using (GetLock())
                    {
                        pool = m_Queue;
                        if (!pool.IsCreated)
                        {
                            pool = m_Queue = new UnsafeChunkedQueue(Memory.PtrSize, Memory.PtrSize * MaxCount, Allocator.Persistent);
                        }
                    }
                }
                return pool;
            }

            public EntityChunk* Get()
            {
                var pool = GetPool();
                using (pool.GetLock())
                {
                    var val = pool.Dequeue().Value;
                    if (val != null)
                    {
                        return *(EntityChunk**)val;
                    }
                }
                return (EntityChunk*)Memory.Malloc<EntityChunk>(1, Allocator.Persistent);
            }

            public void PutBack(EntityChunk* chunk)
            {
                if (chunk == null) return;
                var pool = GetPool();
                using (pool.GetLock())
                {
                    if (pool.Count == MaxCount)
                    {
                        Memory.Free(chunk, Allocator.Persistent);
                    }
                    else
                    {
                        *(EntityChunk**)pool.Enqueue().Value = chunk;
                    }
                }
            }

            private SpinLock GetLock()
            {
                fixed (Instance* ptr = &this)
                {
                    return new SpinLock(&ptr->m_SpinLock);
                }
            }

            public void Dispose()
            {
                if (m_Queue.IsCreated)
                {
                    m_Queue.Dispose();
                    m_Queue = default;
                }
            }
        }

        private static readonly Instance m_Instance;

        private static readonly Instance* m_InstancePtr;

        static EntityChunkPool()
        {
            fixed (Instance* ptr = &m_Instance)
            {
                m_InstancePtr = ptr;
            }
        }

        private static Instance* InstancePtr => m_InstancePtr;

        public static EntityChunk* Get()
        {
            return InstancePtr->Get();
        }

        public static void PutBack(EntityChunk* chunk)
        {
            InstancePtr->PutBack(chunk);
        }

        public static void Dispose()
        {
            InstancePtr->Dispose();
            *InstancePtr = default;
        }
    }
}