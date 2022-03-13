using E.Collections;
using System;
using System.Diagnostics;

namespace E.Entities
{
    internal unsafe struct EntityContainerComponentsData
    {
        #region Main

        private const int HashSetLength = 64;

        private const int RemMask = 0b111111;

        private short m_ComponentCount;

        private ushort m_DataSize;

        private long m_UsedMask;

        private fixed int m_HashSet[HashSetLength];

        public short ComponentCount => m_ComponentCount;

        public ushort DataSize => m_DataSize;

        private struct HashSetInfo
        {
            public short id;
            public ushort offset;
        }

        public void Initialize(ComponentTypeGroup componentTypes)
        {
            this = default;
            InitializeData(componentTypes);
        }

        public void Initialize()
        {
            this = default;
        }

        public int OffsetOf<T>()
            where T : unmanaged, IComponentStructure
            => Find(ComponentTypeIdentity<T>.GetID());

        public int OffsetOf(ComponentType componentType)
            => Find(componentType.ID);

        public int OffsetOf(short id)
            => Find(id);

        public bool TryAdd(ComponentType componentType, out int offset)
        {
            offset = Find(componentType.ID);
            if (offset == -1)
            {
                offset = Insert(componentType);
                return true;
            }
            return false;
        }

        public ComponentTypeGroup GetComponentGroup()
        {
            ComponentTypeGroup group = default;
            var comTypeChunk = ComponentTypeGlobal.ChunkPtr;
            long used = m_UsedMask;
            int infoIndex = 0;
            while (used != 0)
            {
                infoIndex = BitUtility.GetTrailingZerosCount(used);
                HashSetInfo info = GetInfo(infoIndex);
                var comType = comTypeChunk->GetComponentType(info.id);
                group.CombineWith(comType);
                used = BitUtility.RemoveLowestOne(used);
            }
            return group;
        }

        #endregion

        #region Internal

        private void InitializeData(ComponentTypeGroup componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                Insert(componentType);
            }
        }

        private HashSetInfo GetInfo(int index)
        {
            var val = m_HashSet[index];
            return *(HashSetInfo*)&val;
        }

        private void SetInfo(int index, HashSetInfo info)
        {
            m_HashSet[index] = *(int*)&info;
        }

        private int Insert(ComponentType componentType)
        {
            CheckComponentCount();
            int thisComponentSize = componentType.Size;
            if (componentType.Mode == ComponentMode.Singleton)
            {
                thisComponentSize += 8;
            }
            int totalSize = m_DataSize + thisComponentSize;
            int oldChunkIndex = m_DataSize / EntityConst.ChunkSize;
            int newChunkIndex = totalSize / EntityConst.ChunkSize;
            CheckChunkIndex(newChunkIndex);
            ushort offset = (ushort)((oldChunkIndex != newChunkIndex) ? (newChunkIndex * EntityConst.ChunkSize) : m_DataSize);
            totalSize = offset + thisComponentSize;
            long mask = m_UsedMask;
            uint uid = (uint)componentType.ID;
            for (int i = 0; i < HashSetLength; i++)
            {
                int index = (int)((uid + i) & RemMask);
                if (((mask >> index) & 1) == 1)
                {
                    continue;
                }
                else
                {
                    HashSetInfo info = GetInfo(index);
                    info.id = componentType.ID;
                    info.offset = offset;
                    SetInfo(index, info);
                    m_UsedMask |= 1L << index;
                    break;
                }
            }
            m_DataSize = (ushort)totalSize;
            m_ComponentCount++;
            return offset;
        }

        private int Find(short id)
        {
            long mask = m_UsedMask;
            uint uid = (uint)id;
            for (uint i = 0; i < HashSetLength; i++)
            {
                int index = (int)((uid + i) & RemMask);
                if (((mask >> index) & 1) == 1)
                {
                    HashSetInfo info = GetInfo(index);
                    if (id == info.id)
                    {
                        return info.offset;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    return -1;
                }
            }
            return -1;
        }

        #endregion

        #region Check

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckComponentCount()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_ComponentCount >= 64)
            {
                throw new ArgumentException("Too many components, components count must lower then 64.", "componentType");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckChunkIndex(int chunkIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (chunkIndex >= 4)
            {
                throw new ArgumentException("Oversize, total components size max to 65536 byte.", "componentType");
            }
#endif
        }

        #endregion
    }
}