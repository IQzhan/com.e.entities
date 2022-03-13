using System;

namespace E.Entities
{
    /// <summary>
    /// Runtime component type, do not serialize.
    /// </summary>
    public unsafe struct ComponentType : IEquatable<ComponentType>
    {
        public static readonly ComponentType Null = default;

        internal ComponentType(short id, ComponentMode mode, short size)
        {
            m_ID = (short)(id + 1);
            m_Mask = (ushort)((size & 0b11111111111111) | ((int)mode << 14));
        }

        private readonly short m_ID;

        /// |Bits    |Meaning       |Range     |
        /// |[0, 13] |size          |[0, 16383]|
        /// |[14,15] |ComponentMode |[0, 3    ]|
        private readonly ushort m_Mask;

        internal short ID => (short)(m_ID - 1);

        public ComponentMode Mode => (ComponentMode)(m_Mask >> 14);

        public short Size => (short)(m_Mask & 0b11111111111111);

        public Type SystemType => ComponentTypeGlobal.ChunkPtr->GetSystemType(this);

        public static ComponentType TypeOf<T>()
            where T : unmanaged, IComponentStructure
            => ComponentTypeGlobal.ChunkPtr->GetComponentType<T>();

        public static ComponentType TypeOf(Type type)
            => ComponentTypeGlobal.ChunkPtr->GetComponentType(type);

        public override bool Equals(object obj)
            => obj is ComponentType type && Equals(type);

        public bool Equals(ComponentType other)
            => m_ID == other.m_ID && m_Mask == other.m_Mask;

        public override int GetHashCode()
            => HashCode.Combine(m_ID, m_Mask);

        public static bool operator ==(ComponentType left, ComponentType right)
            => left.Equals(right);

        public static bool operator !=(ComponentType left, ComponentType right)
            => !left.Equals(right);

        public static implicit operator ComponentType(Type type) => TypeOf(type);

        public static implicit operator Type(ComponentType componentType) => componentType.SystemType;

        public override string ToString()
            => $"[ComponentType] Name: {SystemType}, RuntimeID: {ID}, Size: {Size}";
    }
}