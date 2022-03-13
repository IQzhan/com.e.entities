using E.Collections.Unsafe;
using System.Threading;
using Unity.Collections;

namespace E.Entities
{
    internal unsafe struct EntityKeyPool
    {
        private int m_KeyOrder;

        /// <summary>
        /// Unused keys.
        /// </summary>
        private UnsafeBitMask m_Mask;

        public void Initialize()
        {
            m_KeyOrder = -1;
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

        public int Get()
        {
            using (m_Mask.GetLock())
            {
                long index = m_Mask.GetFirstThenRemove();
                if (index != -1)
                {
                    return (int)index;
                }
            }
            return Interlocked.Increment(ref m_KeyOrder);
        }

        public void PutBack(int innerKey)
        {
            //innerKey recover
            using (m_Mask.GetLock())
            {
                m_Mask.Set(innerKey, true);
            }
        }

        public void Clear()
        {
            using (m_Mask.GetLock())
            {
                m_KeyOrder = 0;
                m_Mask.Clear();
            }
        }
    }
}