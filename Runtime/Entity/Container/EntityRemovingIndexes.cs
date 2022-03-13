using E.Collections.Unsafe;
using Unity.Collections;

namespace E.Entities
{
    internal struct EntityRemovingIndexes
    {
        /// <summary>
        /// Reversed indexes.
        /// </summary>
        private UnsafeBitMask m_Mask;

        private int m_Size;

        public int Count => m_Mask.Count;

        public void Initialize()
        {
            m_Size = 0;
            m_Mask = new UnsafeBitMask(EntityConst.BitMaskExpandSize, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_Size = 0;
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

        public void ResetSize(int size)
        {
            if (m_Mask.IsNotEmpty())
            {
                m_Mask.Clear();
                size = 0;
            }
            m_Size = size;
        }

        public void Insert(int index)
        {
            using (m_Mask.GetLock())
            {
                int reverseIndex = m_Size - 1 - index;
                m_Mask.Set(reverseIndex, true);
            }
        }

        public int GetFirstThenRemove()
        {
            if (m_Size == 0) return -1;
            int reversedIndex = (int)m_Mask.GetFirstThenRemove();
            if (reversedIndex == -1)
            {
                return -1;
            }
            return m_Size - 1 - reversedIndex;
        }
    }
}