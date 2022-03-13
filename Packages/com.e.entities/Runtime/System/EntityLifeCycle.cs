using System;
using System.Linq;
using Unity.Jobs;

namespace E.Entities
{
    internal sealed class EntityLifeCycle : IDisposable
    {
        internal static EntityLifeCycle instance;

        private IEntitySystem[] m_entitySystems;

        private JobHandle m_handle;

        private bool m_Started;

        private bool m_DisposedValue;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoadAtRuntime()
        {
            instance = new EntityLifeCycle();
            instance.Initialize();
        }

        private void Initialize()
        {
            CollectSystems();
            UnityEngine.Application.quitting -= Dispose;
            UnityEngine.Application.quitting += Dispose;
            var lifeCycleBehaviour = new UnityEngine.GameObject("[EntityLifeCycle]", typeof(EntityLifeCycleBehaviour));
            lifeCycleBehaviour.hideFlags = UnityEngine.HideFlags.HideInInspector | UnityEngine.HideFlags.HideInHierarchy;
        }

        private void CollectSystems()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
            var baseType = typeof(IEntitySystem);
            var allEntitySystemTypes = allTypes
                .Where(t => baseType.IsAssignableFrom(t) &&
                t.IsClass &&
                !t.IsAbstract &&
                !t.IsGenericType);
            m_entitySystems = allEntitySystemTypes
                .Select(t => Activator.CreateInstance(t, true) as IEntitySystem)
                .ToArray();
            ProfilerInfos.EachSystemProfilerInfos = allEntitySystemTypes
                .Select(t => new EntityProfilerInfo($"{t.FullName}"))
                .ToArray();
        }

        internal void Execute()
        {
            using (new EntityProfiler(ProfilerInfos.ExecuteProfilerInfo))
            {
                ExecuteBeforeSystems();
                ExecuteSystems();
            }
        }

        private void ExecuteBeforeSystems()
        {
            using (new EntityProfiler(ProfilerInfos.CompleteProfilerInfo))
            {
                // complete pre frame update.
                m_handle.Complete();
                // complete create and remove.
                m_handle = EntityScene.Complete();
                JobHandle.ScheduleBatchedJobs();
                m_handle.Complete();
                m_handle = default;
                // clear query job data.
                QueryJobAdditionalDataMemory.Clear();
                // Complete added objects.
                ClassReference.Complete();
            }
        }

        private void ExecuteSystems()
        {
            int length = m_entitySystems.Length;
            if (!m_Started)
            {
                using (new EntityProfiler(ProfilerInfos.StartProfilerInfo))
                {
                    for (int i = 0; i < length; i++)
                    {
                        using (new EntityProfiler(ProfilerInfos.EachSystemProfilerInfos[i]))
                        {
                            try
                            {
                                m_entitySystems[i].Start(ref m_handle);
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogException(e);
                            }
                        }
                    }
                }
                m_Started = true;
            }
            using (new EntityProfiler(ProfilerInfos.UpdateProfilerInfo))
            {
                for (int i = 0; i < length; i++)
                {
                    using (new EntityProfiler(ProfilerInfos.EachSystemProfilerInfos[i]))
                    {
                        try
                        {
                            m_entitySystems[i].Update(ref m_handle);
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogException(e);
                        }
                    }
                }
            }
            JobHandle.ScheduleBatchedJobs();
        }

        private void DisposeSystems()
        {
            JobHandle.ScheduleBatchedJobs();
            m_handle.Complete();
            m_handle = default;
            int length = m_entitySystems.Length;
            for (int i = 0; i < length; i++)
            {
                try
                {
                    m_entitySystems[i].Dispose();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
            m_entitySystems = null;
        }

        private void DisposeUnmanaged()
        {
            EntityScene.DisposeEverything();
            QueryJobAdditionalDataMemory.Dispose();
            ClassReference.DisposeEverything();
            EntityChunkPool.Dispose();
            ComponentTypeGlobal.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    ProfilerInfos.EachSystemProfilerInfos = null;
                }
                DisposeSystems();
                DisposeUnmanaged();
                m_DisposedValue = true;
            }
        }

        ~EntityLifeCycle()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}