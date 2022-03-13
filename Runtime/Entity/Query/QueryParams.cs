using Unity.Jobs;

namespace E.Entities
{
    public unsafe struct QueryParams
    {
        /// <summary>
        /// Query componentType ids.
        /// </summary>
        internal fixed byte ids[8];
        
        // 10 1000
        private uint m_Mask;
        
        /// <summary>
        /// Count of query componentTypes.
        /// </summary>
        internal int Count { get => (int)(m_Mask & 0b1111); set => m_Mask = (m_Mask & 0b110000) | (uint)value; }
        
        internal ScheduleMode ScheduleMode { get => (ScheduleMode)(m_Mask >> 4); set => m_Mask = (m_Mask & 0b001111) | (uint)value << 4; }

        internal JobHandle dependsOn;
        internal JobHandle target;

        public static QueryParams Params(ComponentType type0)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
            if (type2 == ComponentType.Null)
            { throw new System.ArgumentNullException("type2"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            ids[2] = (byte)type2.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
            if (type2 == ComponentType.Null)
            { throw new System.ArgumentNullException("type2"); }
            if (type3 == ComponentType.Null)
            { throw new System.ArgumentNullException("type3"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            ids[2] = (byte)type2.ID;
            ids[3] = (byte)type3.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
            if (type2 == ComponentType.Null)
            { throw new System.ArgumentNullException("type2"); }
            if (type3 == ComponentType.Null)
            { throw new System.ArgumentNullException("type3"); }
            if (type4 == ComponentType.Null)
            { throw new System.ArgumentNullException("type4"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            ids[2] = (byte)type2.ID;
            ids[3] = (byte)type3.ID;
            ids[4] = (byte)type4.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4, ComponentType type5)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
            if (type2 == ComponentType.Null)
            { throw new System.ArgumentNullException("type2"); }
            if (type3 == ComponentType.Null)
            { throw new System.ArgumentNullException("type3"); }
            if (type4 == ComponentType.Null)
            { throw new System.ArgumentNullException("type4"); }
            if (type5 == ComponentType.Null)
            { throw new System.ArgumentNullException("type5"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            ids[2] = (byte)type2.ID;
            ids[3] = (byte)type3.ID;
            ids[4] = (byte)type4.ID;
            ids[5] = (byte)type5.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4, ComponentType type5, ComponentType type6)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
            if (type2 == ComponentType.Null)
            { throw new System.ArgumentNullException("type2"); }
            if (type3 == ComponentType.Null)
            { throw new System.ArgumentNullException("type3"); }
            if (type4 == ComponentType.Null)
            { throw new System.ArgumentNullException("type4"); }
            if (type5 == ComponentType.Null)
            { throw new System.ArgumentNullException("type5"); }
            if (type6 == ComponentType.Null)
            { throw new System.ArgumentNullException("type6"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            ids[2] = (byte)type2.ID;
            ids[3] = (byte)type3.ID;
            ids[4] = (byte)type4.ID;
            ids[5] = (byte)type5.ID;
            ids[6] = (byte)type6.ID;
            return queryParams;
        }

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4, ComponentType type5, ComponentType type6, ComponentType type7)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type0 == ComponentType.Null)
            { throw new System.ArgumentNullException("type0"); }
            if (type1 == ComponentType.Null)
            { throw new System.ArgumentNullException("type1"); }
            if (type2 == ComponentType.Null)
            { throw new System.ArgumentNullException("type2"); }
            if (type3 == ComponentType.Null)
            { throw new System.ArgumentNullException("type3"); }
            if (type4 == ComponentType.Null)
            { throw new System.ArgumentNullException("type4"); }
            if (type5 == ComponentType.Null)
            { throw new System.ArgumentNullException("type5"); }
            if (type6 == ComponentType.Null)
            { throw new System.ArgumentNullException("type6"); }
            if (type7 == ComponentType.Null)
            { throw new System.ArgumentNullException("type7"); }
#endif
            QueryParams queryParams = default;
            queryParams.Count = 8;
            byte* ids = queryParams.ids;
            ids[0] = (byte)type0.ID;
            ids[1] = (byte)type1.ID;
            ids[2] = (byte)type2.ID;
            ids[3] = (byte)type3.ID;
            ids[4] = (byte)type4.ID;
            ids[5] = (byte)type5.ID;
            ids[6] = (byte)type6.ID;
            ids[7] = (byte)type7.ID;
            return queryParams;
        }
    }

    internal unsafe struct ContainerQueryParams
    {
        public EntityContainer* container;
        public int length;
        public fixed short offsets[8];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public fixed byte ids[8];
#endif
    }
}