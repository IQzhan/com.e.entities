using E.Collections;
using E.Collections.Unsafe;
using Unity.Collections;

namespace E.Entities
{
    internal unsafe struct QueryJobAdditionalDataMemory
    {
        private static int m_SpinLock;

        private static UnsafeChunkedList m_Data;

        public static void Clear()
        {
            if (m_Data.IsCreated)
            {
                m_Data.Clear();
            }
        }

        public static byte* Require()
        {
            if (!m_Data.IsCreated)
            {
                using (GetLock())
                {
                    if (!m_Data.IsCreated)
                    {
                        m_Data = new UnsafeChunkedList(Memory.SizeOf<ContainerQueryParams>(), 1 << 14, Allocator.Persistent);
                    }
                }
            }
            using (m_Data.GetLock())
            {
                return m_Data.Add().Value;
            }
        }

        public static void Dispose()
        {
            if (m_Data.IsCreated)
            {
                m_Data.Dispose();
                m_Data = default;
            }
        }

        private static SpinLock GetLock()
        {
            fixed (int* ptr = &m_SpinLock)
            {
                return new SpinLock(ptr);
            }
        }
    }
}