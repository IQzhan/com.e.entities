using E.Collections.Unsafe;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using EMemory = E.Collections.Unsafe.Memory;

namespace E.Entities
{
    public unsafe partial struct EntityScene
    {
        private struct Static
        {
            private int m_SpinLock;

            private fixed long m_EntityScenes[EntityConst.MaxSceneCount];

            public EntityScene GetScene(int index)
            {
                CheckIndex(index);
                Instance** scenes = null;
                fixed (long* ptr = m_EntityScenes)
                {
                    scenes = (Instance**)ptr;
                }
                var instance = scenes[index];
                if (instance == null)
                {
                    using (GetLock())
                    {
                        instance = scenes[index];
                        if (instance != null) return new EntityScene(instance);
                        instance = (Instance*)EMemory.Malloc<Instance>(1, Allocator.Persistent);
                        instance->Initialize((short)index);
                        scenes[index] = instance;
                    }
                }
                return new EntityScene(instance);
            }

            public JobHandle Complete()
            {
                JobHandle result = default;
                Instance** scenes = null;
                fixed (long* ptr = m_EntityScenes)
                {
                    scenes = (Instance**)ptr;
                }
                for (int i = 0; i < EntityConst.MaxSceneCount; i++)
                {
                    var instance = scenes[i];
                    if (instance != null)
                    {
                        if (instance->Complete(out JobHandle handle))
                        {
                            result = JobHandle.CombineDependencies(result, handle);
                        }
                    }
                }
                return result;
            }

            public void Dispose()
            {
                Instance** scenes = null;
                fixed (long* ptr = m_EntityScenes)
                {
                    scenes = (Instance**)ptr;
                }
                for (int i = 0; i < EntityConst.MaxSceneCount; i++)
                {
                    var instance = scenes[i];
                    if (instance != null)
                    {
                        instance->Dispose();
                        scenes[i] = null;
                    }
                }
            }

            private SpinLock GetLock()
            {
                fixed (Static* ptr = &this)
                {
                    return new SpinLock(&ptr->m_SpinLock);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckIndex(int index)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index < 0 || index >= EntityConst.MaxSceneCount)
                {
                    throw new IndexOutOfRangeException($"index must > 0 && < {EntityConst.MaxSceneCount}");
                }
#endif
            }
        }

        private static readonly Static m_StaticInstance;

        private static readonly Static* m_StaticInstancePtr;

        static EntityScene()
        {
            fixed (Static* ptr = &m_StaticInstance)
            {
                m_StaticInstancePtr = ptr;
            }
        }

        private static Static* StaticInstancePtr => m_StaticInstancePtr;

        public static EntityScene GetScene(int index = 0)
        {
            CheckPlaying();
            return StaticInstancePtr->GetScene(index);
        }

        internal static EntityScene InternalGetScene(int index = 0)
        {
            return StaticInstancePtr->GetScene(index);
        }

        internal static JobHandle Complete()
        {
            return StaticInstancePtr->Complete();
        }

        internal static void DisposeEverything()
        {
            StaticInstancePtr->Dispose();
            *StaticInstancePtr = default;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckPlaying()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!Application.isPlaying)
            {
                throw new Exception("Only allowed in play mode.");
            }
#endif
        }
    }
}