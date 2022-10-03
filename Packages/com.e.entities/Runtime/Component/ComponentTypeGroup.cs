using E.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace E.Entities
{
    /// <summary>
    /// Runtime component type group, do not serialize.
    /// </summary>
    [DebuggerTypeProxy(typeof(ComponentTypeGroupDebugView))]
    public unsafe partial struct ComponentTypeGroup :
        IEquatable<ComponentTypeGroup>,
        IEnumerable,
        IEnumerable<ComponentType>
    {
        #region Main

        public static readonly ComponentTypeGroup Null = default;

        /// <summary>
        /// 256 bits. The offset of each bit represents the ID of the component type.
        /// </summary>
        private fixed long m_Data[4];

        private short m_Count;

        private short m_Size;

        private ComponentMode m_Mode;

        /// <summary>
        /// How many component types.
        /// </summary>
        public int Count => m_Count;

        /// <summary>
        /// Total size of components.
        /// </summary>
        public int Size => m_Size;

        /// <summary>
        /// Component mode.
        /// </summary>
        public ComponentMode Mode => m_Mode;

        /// <summary>
        /// Add component type.
        /// </summary>
        /// <param name="componentType"></param>
        public void CombineWith(ComponentType componentType)
        {
            if (componentType != ComponentType.Null)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_Count > 0)
                {
                    if (componentType.Mode != m_Mode)
                    {
                        throw new ArgumentException("Component mode is different with included in ComponentTypeGroup.", "componentType");
                    }
                }
#endif
                SetData(componentType.ID, componentType.Size, true);
                m_Mode = componentType.Mode;
            }
        }

        /// <summary>
        /// Remove component type.
        /// </summary>
        /// <param name="componentType"></param>
        public void Remove(ComponentType componentType)
        {
            if (componentType != ComponentType.Null)
            {
                SetData(componentType.ID, componentType.Size, false);
            }
        }

        /// <summary>
        /// Check component type exists in this group.
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public bool Contains(ComponentType componentType)
        {
            if (componentType != ComponentType.Null)
            {
                return GetData(componentType.ID);
            }
            return false;
        }

        public override bool Equals(object obj)
            => obj is ComponentTypeGroup group && Equals(group);

        public bool Equals(ComponentTypeGroup other)
        {
            return m_Data[0] == other.m_Data[0]
                && m_Data[1] == other.m_Data[1]
                && m_Data[2] == other.m_Data[2]
                && m_Data[3] == other.m_Data[3];
        }

        public override int GetHashCode()
            => HashCode.Combine(m_Data[0], m_Data[1], m_Data[2], m_Data[3]);

        public static bool operator ==(ComponentTypeGroup left, ComponentTypeGroup right)
            => left.Equals(right);

        public static bool operator !=(ComponentTypeGroup left, ComponentTypeGroup right)
            => !left.Equals(right);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[ComponentTypeGroup] Count: {Count}, Size: {Size}");
            foreach (var comT in this)
            {
                sb.AppendLine(comT.ToString());
            }
            return sb.ToString();
        }

        #endregion

        #region IEnumerator

        public IDEnumerator GetIDEnumerator() => new IDEnumerator(ref this);

        public Enumerator GetEnumerator() => new Enumerator(ref this);

        IEnumerator<ComponentType> IEnumerable<ComponentType>.GetEnumerator() => new Enumerator(ref this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(ref this);

        public struct IDEnumerator : IEnumerator<short>, IEnumerator, IDisposable
        {
            private readonly ComponentTypeGroup m_Instance;

            private int m_ID;

            public IDEnumerator(ref ComponentTypeGroup instance)
            {
                m_Instance = instance;
                m_ID = -1;
            }

            object IEnumerator.Current => Current;

            public short Current => (short)m_ID;

            public bool MoveNext()
            {
                if (m_ID < 255)
                {
                    int result = m_Instance.GetFirstIDBeginWith(m_ID + 1);
                    if (result != -1)
                    {
                        m_ID = result;
                        return true;
                    }
                    else
                    {
                        m_ID = 255;
                    }
                }
                return false;
            }

            public void Reset() => m_ID = -1;

            public void Dispose() { }
        }

        public struct Enumerator : IEnumerator<ComponentType>, IEnumerator, IDisposable
        {
            private IDEnumerator m_IDEnumerator;

            public Enumerator(ref ComponentTypeGroup instance)
            {
                m_IDEnumerator = new IDEnumerator(ref instance);
            }

            object IEnumerator.Current => Current;

            public ComponentType Current => ComponentTypeGlobal.ChunkPtr->GetComponentType(m_IDEnumerator.Current);

            public bool MoveNext() => m_IDEnumerator.MoveNext();

            public void Reset() => m_IDEnumerator.Reset();

            public void Dispose() { }
        }

        #endregion

        #region Internal

        private void SetData(short id, short size, bool value)
        {
            // id ∈ [0, 255]
            // index = (id / 64) ∈ [0, 3]
            int index = id >> 6;
            // offset = (id % 64) ∈ [0, 63]
            int offset = id & 0b111111;
            var dataValue = m_Data[index];
            var compare = 1L << offset;
            // the original target value at offset.
            bool ori = (dataValue & compare) != 0;
            // is this target value changed?
            bool isDiff = value ^ ori;
            // change value if different.
            if (isDiff)
            {
                if (value)
                {
                    // 0 -> 1
                    dataValue |= compare;
                    m_Size += size;
                    m_Count++;
                }
                else
                {
                    // 1 -> 0
                    dataValue &= ~compare;
                    m_Size -= size;
                    m_Count--;
                }
            }
            m_Data[index] = dataValue;
        }

        private bool GetData(short id)
        {
            int index = id >> 6;
            int offset = id & 0b111111;
            var dataValue = m_Data[index];
            var compare = 1L << offset;
            return (dataValue & compare) != 0;
        }

        private int GetFirstIDBeginWith(int id)
        {
            int index = id >> 6;
            int offset = id & 0b111111;
            var dataValue = m_Data[index];
            var maskedVal = dataValue & (long)(0xFFFFFFFFFFFFFFFF << offset);
            if (maskedVal != 0)
            {
                return (index << 6) + BitUtility.GetTrailingZerosCount(maskedVal);
            }
            else
            {
                for (int innerIndex = index + 1; innerIndex < 4; innerIndex++)
                {
                    var innerDataValue = m_Data[innerIndex];
                    if (innerDataValue != 0)
                    {
                        return (innerIndex << 6) + BitUtility.GetTrailingZerosCount(innerDataValue);
                    }
                }
            }
            return -1;
        }

        #endregion
    }

    #region Debug

    public sealed class ComponentTypeGroupDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ComponentTypeGroup m_Instance;

        public ComponentTypeGroupDebugView(ref ComponentTypeGroup instance)
        {
            m_Instance = instance;
        }

        public int Count => m_Instance.Count;

        public int Size => m_Instance.Size;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ComponentType[] ComponentTypes
        {
            get
            {
                ComponentType[] componentTypes = new ComponentType[Count];
                int index = 0;
                foreach (var comT in m_Instance)
                {
                    componentTypes[index++] = comT;
                }
                return componentTypes;
            }
        }
    }

    #endregion
}