using E.Collections.Unsafe;

namespace E.Entities
{
    /// <summary>
    /// Internal entity data.
    /// </summary>
    internal unsafe struct EntityData
    {
        private uint m_High;

        private int m_Low;

        public bool IsInvalid
        {
            get => (m_High >> 31) == 1;
            set => m_High = (value ? (m_High | 0x80000000) : (m_High & 0x7FFFFFFF));
        }

        public int InnerKey
        {
            get => (int)(m_High & 0x7FFFFFFF);
            set => m_High = (m_High & 0x80000000) | (uint)value;
        }

        public byte* Value
        {
            get
            {
                fixed (EntityData* ptr = &this)
                {
                    return (byte*)ptr + 8;
                }
            }
        }

        public SpinLock GetLock()
        {
            fixed (EntityData* ptr = &this)
            {
                return new SpinLock(&ptr->m_Low);
            }
        }
    }
}