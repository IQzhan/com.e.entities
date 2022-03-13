using System;

namespace E.Entities
{
    /// <summary>
    /// Runtime entity, do not serialize or use as a key.
    /// </summary>
    public unsafe struct Entity : IEquatable<Entity>
    {
        // [23, 31] belongs
        // [0,  22] innerKey
        private readonly uint m_Mask;

        private readonly int m_LatestIndex;

        /// <summary>
        /// Unique key in this scene.
        /// </summary>
        public int Key => (int)m_Mask;

        public short Belongs => (short)(m_Mask >> 23);

        public int InnerKey => (int)(m_Mask & EntityConst.MaxEntityCountEachGroup);

        /// <summary>
        /// May different each frame.
        /// </summary>
        public int LatestIndex => m_LatestIndex;

        internal Entity(short belongs, int innerKey, int latestIndex)
        {
            m_Mask = ((uint)belongs << 23) | (uint)(innerKey & EntityConst.MaxEntityCountEachGroup);
            m_LatestIndex = latestIndex;
        }

        public override bool Equals(object obj)
            => obj is Entity entity && Equals(entity);

        public bool Equals(Entity other)
            => m_Mask == other.m_Mask && m_LatestIndex == other.m_LatestIndex;

        public override int GetHashCode()
            => HashCode.Combine(m_Mask, m_LatestIndex);

        public static bool operator ==(Entity left, Entity right)
            => left.Equals(right);

        public static bool operator !=(Entity left, Entity right)
            => !left.Equals(right);
    }
}