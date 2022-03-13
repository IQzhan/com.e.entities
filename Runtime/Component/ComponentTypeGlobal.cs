using E.Collections.Unsafe;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using SpinLock = E.Collections.Unsafe.SpinLock;

namespace E.Entities
{
    /// <summary>
    /// Runtime id of component type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static unsafe class ComponentTypeIdentity<T>
        where T : unmanaged, IComponentStructure
    {
        private static readonly short* m_IDPtr = ComponentTypeGlobal.RequireIDPtr();

        public static short GetID()
        {
            return (short)(*m_IDPtr - 1);
        }

        public static void SetID(short id)
        {
            *m_IDPtr = (short)(id + 1);
        }
    }

    /// <summary>
    /// Manage runtime component types.
    /// </summary>
    internal static unsafe class ComponentTypeGlobal
    {
        /// <summary>
        /// Will reset while assembly reloaded.
        /// </summary>
        private static readonly ComponentTypesChunk m_Chunk;

        public static readonly ComponentTypesChunk* ChunkPtr;

        static ComponentTypeGlobal()
        {
            fixed (ComponentTypesChunk* ptr = &m_Chunk)
            {
                ChunkPtr = ptr;
            }
        }

        public static short* RequireIDPtr()
            => ChunkPtr->RequireIDPtr();

        public static void Dispose()
            => ChunkPtr->Dispose();

        private struct ComponentTypeData
        {
            public RuntimeTypeHandle typeHandle;

            public ComponentMode mode;

            public short size;
        }

        private struct HashIDMapInfo
        {
            public int hash;

            public short id;
        }

        /// <summary>
        /// Basic component type infomation chunk.
        /// </summary>
        public unsafe struct ComponentTypesChunk
        {
            private int m_SpinLock;

            private int m_IncrementCount;

            private int m_IDsIncrementCount;

            private fixed long m_TypeDatas[EntityConst.MaxComponentTypeCount << 1];

            private fixed int m_HashIDMap[EntityConst.MaxComponentTypeCount << 1];

            private fixed short m_IDs[EntityConst.MaxComponentTypeCount << 1];

            public short* RequireIDPtr()
            {
                int increment = Interlocked.Increment(ref m_IDsIncrementCount);
                CheckIDsIncrement(increment);
                int index = increment - 1;
                fixed (short* ptr = m_IDs)
                {
                    return ptr + index;
                }
            }

            public void Dispose()
            {
                int increment = m_IDsIncrementCount;
                this = default;
                m_IDsIncrementCount = increment;
            }

            public ComponentType GetComponentType<T>()
                where T : unmanaged, IComponentStructure
            {
                //TODO check runtime
                short id = ComponentTypeIdentity<T>.GetID();
                if (id == -1)
                {
                    CheckMaxCount();
                    CheckSize<T>();
                    using (GetLock())
                    {
                        id = ComponentTypeIdentity<T>.GetID();
                        if (id == -1)
                        {
                            Type type = typeof(T);
                            int hashCode = type.GetHashCode();
                            RuntimeTypeHandle typeHandle = type.TypeHandle;
                            ComponentMode mode = GetMode(type);
                            short size = (short)Alignment8(Memory.SizeOf<T>());
                            id = (short)m_IncrementCount;
                            ComponentTypeData typeData = new ComponentTypeData()
                            {
                                typeHandle = typeHandle,
                                mode = mode,
                                size = size
                            };
                            // set values
                            InsertIntoHashIDMap(hashCode, id);
                            SetComponentTypesData(id, typeData);
                            ComponentTypeIdentity<T>.SetID(id);
                            m_IncrementCount++;
                        }
                    }
                }
                return GetComponentType(id);
            }

            public ComponentType GetComponentType(Type type)
            {
                //TODO check runtime
                CheckComponentType(type);
                int hashCode = type.GetHashCode();
                short id = -1;
                using (GetLock())
                {
                    id = FindFromHashIDMap(hashCode);
                }
                if (id == -1)
                {
                    CheckMaxCount();
                    CheckSize(type);
                    using (GetLock())
                    {
                        id = FindFromHashIDMap(hashCode);
                        if (id == -1)
                        {
                            RuntimeTypeHandle typeHandle = type.TypeHandle;
                            ComponentMode mode = GetMode(type);
                            short size = (short)Alignment8(Memory.SizeOf(type));
                            id = (short)m_IncrementCount;
                            ComponentTypeData typeData = new ComponentTypeData()
                            {
                                typeHandle = typeHandle,
                                mode = mode,
                                size = size
                            };
                            // set values
                            InsertIntoHashIDMap(hashCode, id);
                            SetComponentTypesData(id, typeData);
                            SetID(type, id);
                            m_IncrementCount++;
                        }
                    }
                }
                return GetComponentType(id);
            }

            public Type GetSystemType(ComponentType componentType)
            {
                if (componentType != ComponentType.Null)
                {
                    var typeData = GetComponentTypesData(componentType.ID);
                    return Type.GetTypeFromHandle(typeData.typeHandle);
                }
                return null;
            }

            public ComponentType GetComponentType(short id)
            {
                if (id < 0 || id >= EntityConst.MaxComponentTypeCount) return ComponentType.Null;
                var typeData = GetComponentTypesData(id);
                if (typeData.size != 0)
                {
                    return new ComponentType(id, typeData.mode, typeData.size);
                }
                return ComponentType.Null;
            }

            private ComponentMode GetMode(Type type)
            {
                if (typeof(IComponent).IsAssignableFrom(type))
                {
                    return ComponentMode.Instance;
                }
                else if (typeof(ISingletonComponent).IsAssignableFrom(type))
                {
                    return ComponentMode.Singleton;
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new ArgumentException("Component type not available", "type");
#endif
            }

            private void SetID(Type type, short id)
            {
                var method = typeof(ComponentTypeIdentity<>).MakeGenericType(type)
                    .GetMethod("SetID", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[1] { id });
            }

            private int Alignment8(int size)
            {
                int rem = size & 0b111;
                return rem == 0 ? size : (size + (8 - rem));
            }

            private ComponentTypeData GetComponentTypesData(short id)
            {
                fixed (long* ptr = m_TypeDatas)
                {
                    return *(((ComponentTypeData*)ptr) + id);
                }
            }

            private void SetComponentTypesData(short id, ComponentTypeData value)
            {
                fixed (long* ptr = m_TypeDatas)
                {
                    *(((ComponentTypeData*)ptr) + id) = value;
                }
            }

            private short InsertIntoHashIDMap(int hash, short id)
            {
                fixed (int* ptr = m_HashIDMap)
                {
                    HashIDMapInfo* infos = (HashIDMapInfo*)ptr;
                    for (uint i = 0; i < EntityConst.MaxComponentTypeCount; i++)
                    {
                        uint index = ((uint)hash + i) & EntityConst.MaxComponentTypeCountRemMask;
                        var info = infos[index];
                        if (info.hash == hash)
                        {
                            return info.id;
                        }
                        else if (info.hash == 0)
                        {
                            info.hash = hash;
                            info.id = id;
                            infos[index] = info;
                            return info.id;
                        }
                    }
                }
                return -1;
            }

            private short FindFromHashIDMap(int hash)
            {
                fixed (int* ptr = m_HashIDMap)
                {
                    HashIDMapInfo* infos = (HashIDMapInfo*)ptr;
                    for (uint i = 0; i < EntityConst.MaxComponentTypeCount; i++)
                    {
                        uint index = ((uint)hash + i) & EntityConst.MaxComponentTypeCountRemMask;
                        var info = infos[index];
                        if (info.hash == hash)
                        {
                            return info.id;
                        }
                        else if (info.hash == 0)
                        {
                            return -1;
                        }
                    }
                }
                return -1;
            }

            private SpinLock GetLock()
            {
                fixed (ComponentTypesChunk* ptr = &this)
                {
                    return new SpinLock(&ptr->m_SpinLock);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void CheckIDsIncrement(int increment)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (increment >= EntityConst.MaxComponentTypeCount << 1)
                {
                    throw new Exception("Too many IComponentStructure");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void CheckSize<T>()
                where T : unmanaged, IComponentStructure
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (Memory.SizeOf<T>() > (EntityConst.ChunkSize - 8))
                {
                    throw new ArgumentException($"{typeof(T)} Size cannot be greater than {EntityConst.ChunkSize - 8}");
                }
#endif
            }

            private static void CheckSize(Type type)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (Memory.SizeOf(type) > (EntityConst.ChunkSize - 8))
                {
                    throw new ArgumentException($"{type} Size cannot be greater than {EntityConst.ChunkSize - 8}");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void CheckComponentType(Type type)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Memory.CheckType(type);
                if (!typeof(IComponentStructure).IsAssignableFrom(type))
                {
                    throw new ArgumentException($"{type} must implement {nameof(IComponentStructure)}.");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckMaxCount()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_IncrementCount >= EntityConst.MaxComponentTypeCount)
                {
                    throw new Exception($"Too many component types. Component types count max to {EntityConst.MaxComponentTypeCount}.");
                }
#endif
            }
        }
    }
}