namespace E.Entities
{
    public static class Static
    {
        public static EntityScene GetScene(int index = 0)
            => EntityScene.GetScene(index);

        public static ComponentType TypeOf<T>()
            where T : unmanaged, IComponentStructure
            => ComponentType.TypeOf<T>();

        public static ComponentType TypeOf(System.Type type)
            => ComponentType.TypeOf(type);

        public static ComponentTypeGroup Combine(ComponentType t0)
            => ComponentTypeGroup.Combine(t0);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1)
            => ComponentTypeGroup.Combine(t0, t1);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2)
            => ComponentTypeGroup.Combine(t0, t1, t2);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9, ComponentType t10)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9, ComponentType t10, ComponentType t11)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9, ComponentType t10, ComponentType t11, ComponentType t12)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9, ComponentType t10, ComponentType t11, ComponentType t12, ComponentType t13)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9, ComponentType t10, ComponentType t11, ComponentType t12, ComponentType t13, ComponentType t14)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);

        public static ComponentTypeGroup Combine(ComponentType t0, ComponentType t1, ComponentType t2, ComponentType t3, ComponentType t4, ComponentType t5, ComponentType t6, ComponentType t7, ComponentType t8, ComponentType t9, ComponentType t10, ComponentType t11, ComponentType t12, ComponentType t13, ComponentType t14, ComponentType t15)
            => ComponentTypeGroup.Combine(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);

        public static QueryParams Params(ComponentType type0)
            => QueryParams.Params(type0);

        public static QueryParams Params(ComponentType type0, ComponentType type1)
            => QueryParams.Params(type0, type1);

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2)
            => QueryParams.Params(type0, type1, type2);

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3)
            => QueryParams.Params(type0, type1, type2, type3);

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4)
            => QueryParams.Params(type0, type1, type2, type3, type4);

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4, ComponentType type5)
            => QueryParams.Params(type0, type1, type2, type3, type4, type5);

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4, ComponentType type5, ComponentType type6)
            => QueryParams.Params(type0, type1, type2, type3, type4, type5, type6);

        public static QueryParams Params(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4, ComponentType type5, ComponentType type6, ComponentType type7)
            => QueryParams.Params(type0, type1, type2, type3, type4, type5, type6, type7);
    }
}