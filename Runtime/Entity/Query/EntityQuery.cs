using System;
using System.Diagnostics;
using Unity.Jobs;

namespace E.Entities
{
    public unsafe partial struct EntityQuery : IEquatable<EntityQuery>
    {
        private readonly EntityScene.Instance* m_Scene;

        private readonly ComponentTypeGroup m_SearchTargets;

        public bool IsCreated => (m_Scene != null) && m_Scene->IsCreated;

        internal EntityQuery(EntityScene.Instance* entityScene, ComponentTypeGroup componentTypes)
        {
            m_Scene = entityScene;
            m_SearchTargets = componentTypes;
        }

        internal JobHandle ForEach4<Callback>(ref Callback callback, ref QueryParams queryParams, ScheduleMode scheduleMode, JobHandle dependsOn)
            where Callback : struct, IEntityQueryCallback4
        {
            CheckExists();
            queryParams.ScheduleMode = scheduleMode;
            queryParams.dependsOn = dependsOn;
            queryParams.target = dependsOn;
            m_Scene->QueryEntities4(ref callback, ref queryParams, m_SearchTargets);
            return queryParams.target;
        }

        internal JobHandle ForEach8<Callback>(ref Callback callback, ref QueryParams queryParams, ScheduleMode scheduleMode, JobHandle dependsOn)
            where Callback : struct, IEntityQueryCallback8
        {
            CheckExists();
            queryParams.ScheduleMode = scheduleMode;
            queryParams.dependsOn = dependsOn;
            queryParams.target = dependsOn;
            m_Scene->QueryEntities8(ref callback, ref queryParams, m_SearchTargets);
            return queryParams.target;
        }

        internal void ForEachGroups<Callback>(ref Callback callback)
            where Callback : struct, IEntityGroupQueryCallback
        {
            CheckExists();
            m_Scene->QueryGroups(ref callback, m_SearchTargets);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new NullReferenceException($"{nameof(EntityQuery)} is yet created or already disposed.");
            }
#endif
        }

        public override bool Equals(object obj)
            => obj is EntityQuery query && Equals(query);

        public bool Equals(EntityQuery other)
            => m_Scene == other.m_Scene && m_SearchTargets.Equals(other.m_SearchTargets);

        public override int GetHashCode()
            => HashCode.Combine((long)m_Scene, m_SearchTargets);

        public static bool operator ==(EntityQuery left, EntityQuery right)
            => left.Equals(right);

        public static bool operator !=(EntityQuery left, EntityQuery right)
            => !left.Equals(right);
    }

    public unsafe static class EntityQueryExtends_Entities4
    {
        public static JobHandle ForEach<Callback>(ref this EntityQuery query, QueryParams queryParams, Callback callback, ScheduleMode scheduleMode, JobHandle dependsOn = default)
            where Callback : struct, IEntityQueryCallback4
        {
            return query.ForEach4(ref callback, ref queryParams, scheduleMode, dependsOn);
        }
    }

    public unsafe static class EntityQueryExtends_Entities8
    {
        public static JobHandle ForEach<Callback>(ref this EntityQuery query, QueryParams queryParams, Callback callback, ScheduleMode scheduleMode, JobHandle dependsOn = default)
            where Callback : struct, IEntityQueryCallback8
        {
            return query.ForEach8(ref callback, ref queryParams, scheduleMode, dependsOn);
        }
    }

    public unsafe static class EntityQueryExtends_Group
    {
        public static void ForEach<Callback>(ref this EntityQuery query, Callback callback)
            where Callback : struct, IEntityGroupQueryCallback
        {
            query.ForEachGroups(ref callback);
        }
    }
}