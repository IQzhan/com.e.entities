using E.Collections;
using E.Collections.Unsafe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;

namespace E.Entities
{
    /// <summary>
    /// Connnect managed and unmanaged code. Don't overuse.
    /// </summary>
    public unsafe sealed class ClassReference : IDisposable
    {
        private const int MaxRegisteredCount = ushort.MaxValue;

        private static ClassReference m_Instance;

        private static int m_SpinLock;

        private Dictionary<object, ushort> m_ObjectIndexMap;

        private UnsafeBitMask m_Unused;

        private List<object> m_Objects;

        private List<object> m_AddedObjects;

        private bool m_DisposedValue;

        /// <summary>
        /// Register from managed code.
        /// </summary>
        /// <typeparam name="Class"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static ClassReference<Class> Register<Class>(Class obj)
            where Class : class
        {
            CreateInstance();
            return m_Instance.InternalRegister(obj);
        }

        /// <summary>
        /// Unregister from managed code.
        /// </summary>
        /// <typeparam name="Class"></typeparam>
        /// <param name="obj"></param>
        public static void Unregister<Class>(Class obj)
            where Class : class
        {
            CheckExists();
            m_Instance.InternalUnregister(obj);
        }

        /// <summary>
        /// Unregister from managed code.
        /// </summary>
        /// <typeparam name="Class"></typeparam>
        /// <param name="reference"></param>
        public static void Unregister<Class>(ClassReference<Class> reference)
            where Class : class
        {
            CheckExists();
            m_Instance.InternalUnregister(reference);
        }

        /// <summary>
        /// Get registered object.
        /// </summary>
        /// <typeparam name="Class"></typeparam>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Class GetRegistered<Class>(ClassReference<Class> reference)
            where Class : class
        {
            CheckExists();
            return m_Instance.InternalGetRegistered(reference);
        }

        internal static void Complete()
        {
            if (m_Instance == null) return;
            m_Instance.InternalComplete();
        }

        internal static void DisposeEverything()
        {
            if (m_Instance != null)
            {
                m_Instance.Dispose();
                m_Instance = null;
            }
        }

        private static void CreateInstance()
        {
            if (m_Instance == null)
            {
                using (GetLock())
                {
                    if (m_Instance == null)
                    {
                        m_Instance = new ClassReference();
                        m_Instance.Initialize();
                    }
                }
            }
        }

        private static SpinLock GetLock()
        {
            fixed (int* ptr = &m_SpinLock)
            {
                return new SpinLock(ptr);
            }
        }

        private void Initialize()
        {
            m_ObjectIndexMap = new Dictionary<object, ushort>();
            m_Unused = new UnsafeBitMask(EntityConst.BitMaskExpandSize, Allocator.Persistent);
            m_Objects = new List<object>();
            m_AddedObjects = new List<object>();
        }

        private ClassReference<Class> InternalRegister<Class>(Class obj)
            where Class : class
        {
            CheckNull(obj);
            CheckMaxCount();
            // if not exists then add
            using (GetLock())
            {
                if (m_ObjectIndexMap.TryGetValue(obj, out var index))
                {
                    return new ClassReference<Class>() { Index = index };
                }
                int unusedIndex = GetUnused();
                if (unusedIndex != -1)
                {
                    m_Objects[unusedIndex] = obj;
                }
                else
                {
                    m_AddedObjects.Add(obj);
                    unusedIndex = m_Objects.Count + m_AddedObjects.Count - 1;
                }
                m_ObjectIndexMap.Add(obj, (ushort)unusedIndex);
                return new ClassReference<Class> { Index = unusedIndex };
            }
        }

        private void InternalUnregister<Class>(Class obj)
            where Class : class
        {
            CheckNull(obj);
            // if exists then remove
            using (GetLock())
            {
                if (m_ObjectIndexMap.TryGetValue(obj, out var index))
                {
                    m_Objects[index] = null;
                    m_ObjectIndexMap.Remove(obj);
                    PutBack(index);
                }
            }
        }

        private void InternalUnregister<Class>(ClassReference<Class> reference)
            where Class : class
        {
            CheckNull(reference);
            CheckIndex(reference.Index);
            // if exists then remove
            using (GetLock())
            {
                int index = reference.Index;
                var obj = m_Objects[index];
                if (obj != null)
                {
                    m_ObjectIndexMap.Remove(obj);
                    m_Objects[index] = null;
                    PutBack(index);
                }
            }
        }

        private Class InternalGetRegistered<Class>(ClassReference<Class> reference)
            where Class : class
        {
            CheckNull(reference);
            CheckIndex(reference.Index);
            int index = reference.Index;
            var obj = m_Objects[index];
            return (Class)obj;
        }

        private int GetUnused()
        {
            int unusedIndex = (int)m_Unused.GetFirstThenRemove();
            return unusedIndex;
        }

        private void PutBack(int index)
        {
            int max = index + 1;
            if (max > m_Unused.Capacity)
            {
                int expsize = max - (int)m_Unused.Capacity;
                int rem = expsize % EntityConst.BitMaskExpandSize;
                expsize = rem == 0 ? expsize : (expsize + (EntityConst.BitMaskExpandSize - rem));
                m_Unused.Expand(expsize);
            }
            m_Unused.Set(index, true);
        }

        private void InternalComplete()
        {
            // combine list
            if (m_AddedObjects.Count > 0)
            {
                m_Objects.AddRange(m_AddedObjects);
                m_AddedObjects.Clear();
            }
        }

        private void DisposeManaged()
        {
            m_ObjectIndexMap.Clear();
            m_Objects.Clear();
            m_AddedObjects.Clear();
            m_ObjectIndexMap = default;
            m_Objects = default;
            m_AddedObjects = default;
        }

        private void DisposeUnmanaged()
        {
            m_Unused.Dispose();
            m_Unused = default;
        }

        private void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    DisposeManaged();
                }
                DisposeUnmanaged();
                m_DisposedValue = true;
            }
        }

        ~ClassReference()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Check

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_Instance == null)
            {
                throw new NullReferenceException("Not created.");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckMaxCount()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_Objects.Count + m_AddedObjects.Count >= MaxRegisteredCount)
            {
                throw new IndexOutOfRangeException("Too many ClassReference");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckNull<Class>(Class obj)
            where Class : class
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckNull<Class>(ClassReference<Class> reference)
            where Class : class
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (reference == ClassReference<Class>.Null)
            {
                throw new ArgumentNullException("reference");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckIndex(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index >= m_Objects.Count)
            {
                throw new IndexOutOfRangeException($"index must be less than {m_Objects.Count} in this frame.");
            }
#endif
        }

        #endregion
    }

    public struct ClassReference<Class>
        : IEquatable<ClassReference<Class>>
        where Class : class
    {
        public static readonly ClassReference<Class> Null = default;

        internal ushort m_Index;

        internal int Index { get => m_Index - 1; set => m_Index = (ushort)(value + 1); }

        public bool IsCreated => m_Index > 0;

        public Class Object => IsCreated ? ClassReference.GetRegistered(this) : default;

        public override bool Equals(object obj)
            => obj is ClassReference<Class> reference && Equals(reference);

        public bool Equals(ClassReference<Class> other)
            => m_Index == other.m_Index;

        public override int GetHashCode()
            => HashCode.Combine(m_Index);

        public static bool operator ==(ClassReference<Class> left, ClassReference<Class> right)
            => left.Equals(right);

        public static bool operator !=(ClassReference<Class> left, ClassReference<Class> right)
            => !(left == right);
    }
}