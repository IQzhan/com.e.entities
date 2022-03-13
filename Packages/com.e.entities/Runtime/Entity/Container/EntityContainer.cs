using E.Collections;
using E.Collections.Unsafe;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace E.Entities
{
    /// <summary>
    /// Entity container.
    /// </summary>
    internal unsafe struct EntityContainer
    {
        #region Properties

        /// <summary>
        /// Unique id.
        /// 0 nothing
        /// 1 singleton
        /// 2 - 481 
        /// </summary>
        private short m_ID;

        public short ID { get => (short)(m_ID - 1); set => m_ID = (short)(value + 1); }

        public bool IsCreated => m_ID > 0;

        /// <summary>
        /// Size of each entity.
        /// </summary>
        public short entitySize;

        /// <summary>
        /// How many entities in one chunk.
        /// </summary>
        public short entityCountPerChunk;

        /// <summary>
        /// Size of chunk list.
        /// </summary>
        public short chunkListSize;
        public short appendChunkListSize;

        /// <summary>
        /// How many chunks.
        /// </summary>
        public short chunkCount;
        public short appendChunkCountMid;
        public short appendChunkCount;

        /// <summary>
        /// Count of entity.
        /// </summary>
        public int entityCount;
        public int appendEntityCountMid;
        public int appendEntityCount;

        /// <summary>
        /// How many entities will be created in this frame.
        /// </summary>
        public int expectedCreatedCount;

        /// <summary>
        /// Spin lock.
        /// </summary>
        public int spinLock;

        /// <summary>
        /// Chunk list.
        /// </summary>
        public EntityChunk** chunkList;
        public EntityChunk** appendChunkList;

        /// <summary>
        /// Components data.
        /// </summary>
        public EntityContainerComponentsData componentsData;

        /// <summary>
        /// Inner key pool.
        /// </summary>
        public EntityKeyPool keyPool;

        /// <summary>
        /// Indexes to remove.
        /// </summary>
        public EntityRemovingIndexes removingIndexes;

        /// <summary>
        /// Keys removed in previous frame.
        /// </summary>
        public EntityRemovedKeys removedKeys;

        #endregion

        #region Initialize & Dispose

        /// <summary>
        /// Initialize this container.
        /// </summary>
        /// <param name="componentTypes"></param>
        public void Initialize(short id, ComponentTypeGroup componentTypes)
        {
            this = default;
            ID = id;
            if (id > 0)
            {
                // not singleton
                entitySize = (short)(8 + componentTypes.Size);
                entityCountPerChunk = (short)(EntityConst.ChunkSize / entitySize);
                componentsData.Initialize(componentTypes);
                keyPool.Initialize();
                removingIndexes.Initialize();
                removedKeys.Initialize();
            }
            else
            {
                // singleton
                componentsData.Initialize();
            }
        }

        /// <summary>
        /// Dispose this container.
        /// </summary>
        public void Dispose()
        {
            keyPool.Dispose();
            removingIndexes.Dispose();
            removedKeys.Dispose();
            if (chunkList != null)
            {
                for (int i = 0; i < chunkCount + appendChunkCountMid; i++)
                {
                    EntityChunkPool.PutBack(chunkList[i]);
                }
                Memory.Free(chunkList, Allocator.Persistent);
            }
            if (appendChunkList != null)
            {
                for (int i = 0; i < appendChunkCount; i++)
                {
                    EntityChunkPool.PutBack(appendChunkList[i]);
                }
                Memory.Free(appendChunkList, Allocator.Persistent);
            }
            this = default;
        }

        #endregion

        #region Exposed

        /// <summary>
        /// Get or create a singleton entity.
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public EntityComponentsData GetSingleton(ComponentType componentType)
        {
            CheckNull(componentType);
            var entityData = RequireComponentData(componentType, out int offset);
            return NewEntityComponentsData(entityData, offset);
        }

        /// <summary>
        /// Prepare chunks for create, must Call outside jobs before create entities.
        /// </summary>
        /// <param name="count"></param>
        public void WillCreate(int count)
        {
            if (count < 1) return;
            RequireChunks(count);
            int maxEntityCount = (chunkCount + appendChunkCountMid + appendChunkCount) * entityCountPerChunk;
            keyPool.SetMaxCount(maxEntityCount);
            removingIndexes.SetMaxCount(maxEntityCount);
            removedKeys.SetMaxCount(maxEntityCount);
        }

        /// <summary>
        /// Create an entity.
        /// </summary>
        /// <returns></returns>
        public EntityComponentsData CreateEntity()
        {
            EntityData* entityData = RequireEntityData(out int index);
            *entityData = default;
            // get a new key.
            entityData->InnerKey = keyPool.Get();
            // let programer set vallues.
            return NewEntityComponentsData(entityData, index);
        }

        /// <summary>
        /// Remove an entity.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EntityComponentsData RemoveEntity(int index)
        {
            CheckIndex(index);
            // mark as deleted
            EntityData* entityData = InternalGet(chunkList, index);
            entityData->IsInvalid = true;
            // add to removing indexes.
            removingIndexes.Insert(index);
            // let programer dispose values.
            return NewEntityComponentsData(entityData, index);
        }

        /// <summary>
        /// Get an entity.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EntityComponentsData GetEntity(int index)
        {
            CheckIndex(index);
            EntityData* entityData = InternalGet(chunkList, index);
            return NewEntityComponentsData(entityData, index);
        }

        /// <summary>
        /// Get removed entities in the previous frame.
        /// </summary>
        /// <returns></returns>
        public RemovedEntities GetRemovedEntities()
            => new RemovedEntities(ID, removedKeys);

        /// <summary>
        /// decide to call CompleteRemove() in job or in main thread.
        /// </summary>
        /// <returns></returns>
        public int GetRemovingAndRemovedCount()
        {
            return removingIndexes.Count + removedKeys.Count;
        }

        /// <summary>
        /// Complete create.
        /// </summary>
        public void CompleteCreateEntities()
        {
            expectedCreatedCount = 0;
            // combine entityCount appendEntityCountMid
            if (appendEntityCountMid > 0)
            {
                entityCount += appendEntityCountMid;
                appendEntityCountMid = 0;
            }
            if (appendChunkCountMid > 0)
            {
                chunkCount += appendChunkCountMid;
                appendChunkCountMid = 0;
            }
            // combine chunkList and appendChunkList
            if (appendChunkCount > 0)
            {
                DoCreateEntities();
            }
        }

        /// <summary>
        /// Complete remove.
        /// </summary>
        public void CompleteRemoveEntities()
        {
            // recover keys
            if (removedKeys.Count > 0)
            {
                int removedKey = 0;
                while ((removedKey = removedKeys.GetFirstThenRemove()) != -1)
                {
                    keyPool.PutBack(removedKey);
                }
            }
            // remove
            if (removingIndexes.Count > 0)
            {
                DoRemoveEntities();
            }
            // reset size
            removingIndexes.ResetSize(entityCount);
        }

        /// <summary>
        /// foreach
        /// </summary>
        /// <param name="startInclude"></param>
        /// <param name="endExclude"></param>
        /// <param name="chunk"></param>
        /// <param name="chunkStartInclude"></param>
        /// <param name="innerStartInclude"></param>
        /// <param name="innerEndExclude"></param>
        /// <returns></returns>
        public bool GetRange(ref int startInclude, ref int endExclude,
            out EntityChunk* chunk, out int chunkStartInclude,
            out int innerStartInclude, out int innerEndExclude)
        {
            endExclude = endExclude > entityCount ? entityCount : endExclude;
            if (startInclude >= endExclude)
            {
                chunk = default;
                chunkStartInclude = default;
                innerStartInclude = default;
                innerEndExclude = default;
                return false;
            }
            int chunkIndex = startInclude / entityCountPerChunk;
            innerStartInclude = startInclude % entityCountPerChunk;
            chunk = *(chunkList + chunkIndex);
            chunkStartInclude = chunkIndex * entityCountPerChunk;
            int step = math.min(entityCountPerChunk - innerStartInclude, endExclude - startInclude); ;
            innerEndExclude = innerStartInclude + step;
            startInclude += step;
            return true;
        }

        #endregion

        #region Internal

        private void RequireChunks(int createEntityCount)
        {
            using (GetLock())
            {
                expectedCreatedCount += createEntityCount;
                int totalEntityCount = entityCount + expectedCreatedCount;
                int targetChunkCount = (totalEntityCount / entityCountPerChunk)
                    + ((totalEntityCount % entityCountPerChunk) == 0 ? 0 : 1);
                // add chunk to chunkList
                int maxChunkCount = targetChunkCount > chunkListSize ? chunkListSize : targetChunkCount;
                for (int i = chunkCount + appendChunkCountMid; i < maxChunkCount; i++)
                {
                    *(chunkList + chunkCount + appendChunkCountMid++) = EntityChunkPool.Get();
                }
                // expand appendChunkList
                ExpandChunkList(ref appendChunkList, ref appendChunkListSize, targetChunkCount - (chunkListSize + appendChunkListSize));
                // add chunk to appendChunkList
                for (int i = chunkCount + appendChunkCountMid + appendChunkCount; i < targetChunkCount; i++)
                {
                    *(appendChunkList + appendChunkCount++) = EntityChunkPool.Get();
                }
            }
        }

        private EntityData* RequireComponentData(ComponentType componentType, out int offset)
        {
            EntityData* entityData = null;
            using (GetLock())
            {
                bool justCreated = false;
                if (componentsData.TryAdd(componentType, out offset))
                {
                    justCreated = true;
                }
                // expand by offset
                int chunkIndex = offset / EntityConst.ChunkSize;
                int indexInChunk = offset % EntityConst.ChunkSize;
                if (chunkIndex >= chunkCount)
                {
                    var targetChunkCount = chunkCount + 1;
                    if (targetChunkCount > chunkListSize)
                    {
                        ExpandChunkList(ref chunkList, ref chunkListSize, 1);
                    }
                    *(chunkList + chunkCount) = EntityChunkPool.Get();
                    chunkCount = (short)targetChunkCount;
                }
                entityData = (EntityData*)((*(chunkList + chunkIndex))->data + indexInChunk);
                if (justCreated)
                {
                    *entityData = default;
                    entityData->InnerKey = entityCount++;
                }
            }
            return entityData;
        }

        private EntityData* RequireEntityData(out int index)
        {
            EntityChunk** from = null;
            int fromIndex = -1;
            using (GetLock())
            {
                int maxCountInChunkList = (chunkCount + appendChunkCountMid) * entityCountPerChunk;
                int maxCount = maxCountInChunkList + appendChunkCount * entityCountPerChunk;
                index = entityCount + appendEntityCountMid + appendEntityCount;
                if (index < maxCountInChunkList)
                {
                    from = chunkList;
                    fromIndex = entityCount + appendEntityCountMid++;
                }
                else if (index < maxCount)
                {
                    from = appendChunkList;
                    fromIndex = appendEntityCount++;
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                else
                {
                    ThrowTooManyEntities();
                }
#endif
            }
            return InternalGet(from, fromIndex);
        }

        internal EntityData* InternalGet(EntityChunk** chunkList, int index)
        {
            int chunkIndex = index / entityCountPerChunk;
            int indexInChunk = index % entityCountPerChunk;
            return (EntityData*)((*(chunkList + chunkIndex))->data + indexInChunk * entitySize);
        }

        internal EntityData* InternalGet(int offset)
        {
            int chunkIndex = offset / EntityConst.ChunkSize;
            int indexInChunk = offset % EntityConst.ChunkSize;
            return (EntityData*)((*(chunkList + chunkIndex))->data + indexInChunk);
        }

        internal EntityComponentsData NewEntityComponentsData(EntityData* data, int position)
        {
            fixed (EntityContainer* container = &this)
            {
                return new EntityComponentsData(container, data, position);
            }
        }

        private void DoCreateEntities()
        {
            short newChunkListSize = (short)(chunkCount + appendChunkCount);
            int rem = newChunkListSize & 0b11111;
            newChunkListSize = (short)(rem == 0 ? newChunkListSize : (newChunkListSize + (EntityConst.ChunkListExpandSize - rem)));
            var oldList = chunkList;
            var newList = (EntityChunk**)Memory.Malloc(newChunkListSize * Memory.PtrSize, 1, Allocator.Persistent);
            for (int i = 0; i < newChunkListSize; i++)
            {
                newList[i] = default;
            }
            if (oldList != null)
            {
                Memory.Copy(newList, oldList, chunkCount * Memory.PtrSize);
                Memory.Free(oldList, Allocator.Persistent);
            }
            Memory.Copy(newList + chunkCount, appendChunkList, appendChunkCount * Memory.PtrSize);
            for (int i = 0; i < appendChunkCount; i++)
            {
                appendChunkList[i] = default;
            }
            chunkList = newList;
            chunkListSize = newChunkListSize;
            chunkCount += appendChunkCount;
            appendChunkCount = 0;
            entityCount += appendEntityCount;
            appendEntityCount = 0;
        }

        private void DoRemoveEntities()
        {
            int removeIndex = 0;
            while ((removeIndex = removingIndexes.GetFirstThenRemove()) != -1)
            {
                var thisEntityData = InternalGet(chunkList, removeIndex);
                int key = thisEntityData->InnerKey;
                int lastIndex = entityCount - 1;
                if (removeIndex != lastIndex)
                {
                    //move last to this
                    Memory.Copy(thisEntityData, InternalGet(chunkList, lastIndex), entitySize);
                }
                removedKeys.Insert(key);
                entityCount = lastIndex;
            }
            //trim end after remove.
            TrimEnd();
        }

        private void ExpandChunkList(ref EntityChunk** list, ref short listSize, int expandSize)
        {
            // Rounding
            if (expandSize <= 0) return;
            int rem = expandSize % EntityConst.ChunkListExpandSize;
            expandSize = rem == 0 ? expandSize : (expandSize + (EntityConst.ChunkListExpandSize - rem));
            // Expand
            var oldChunkListSize = listSize;
            var oldChunkList = list;
            var newChunkListSize = (short)(oldChunkListSize + expandSize);
            var newChunkList = (EntityChunk**)Memory.Malloc(Memory.PtrSize * newChunkListSize, 1, Allocator.Persistent);
            for (int i = 0; i < newChunkListSize; i++)
            {
                newChunkList[i] = default;
            }
            if (oldChunkList != null)
            {
                Memory.Copy(newChunkList, oldChunkList, Memory.PtrSize * oldChunkListSize);
                Memory.Free(oldChunkList, Allocator.Persistent);
            }
            list = newChunkList;
            listSize = newChunkListSize;
        }

        private void TrimEnd()
        {
            int rem = entityCount % entityCountPerChunk;
            int expectedChunkCount = (entityCount / entityCountPerChunk) + (rem == 0 ? 0 : 1);
            while (chunkCount > expectedChunkCount)
            {
                int i = chunkCount - 1;
                ref var chunk = ref *(chunkList + i);
                EntityChunkPool.PutBack(chunk);
                chunkCount = (short)i;
                chunk = default;
            }
        }

        private SpinLock GetLock()
        {
            fixed (EntityContainer* ptr = &this)
            {
                return new SpinLock(&ptr->spinLock);
            }
        }

        #endregion

        #region Check

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ThrowTooManyEntities()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            throw new IndexOutOfRangeException("Trying to create too many entities. Call WillCreate(int count) before create entities.");
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private void CheckNull(ComponentType componentType)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentType == ComponentType.Null)
            {
                throw new ArgumentException("Can not be default.", "componentType");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private void CheckIndex(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= entityCount)
            {
                throw new IndexOutOfRangeException($"{nameof(EntityContainer)} index must in range [0, {entityCount}).");
            }
#endif
        }

        #endregion
    }
}