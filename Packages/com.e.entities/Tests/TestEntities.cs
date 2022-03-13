using E.Collections.Unsafe;
using E.Entities;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace E.Test
{
    public unsafe class TestEntities
    {
        private struct TestSingleton0Data : ISingletonComponent
        {
            public float a;
        }

        private struct TestSingleton1Data : ISingletonComponent
        {
            public float a;
        }

        private struct TestFuckingLargeSingletonData : ISingletonComponent
        {
            public fixed byte data[EntityConst.ChunkSize - 8];
        }

        [Test]
        public void Test0_ComponentType()
        {
            // Compare: Type then Generic.
            var compareType_0_0 = ComponentType.TypeOf(typeof(PositionData));
            var compareType_0_1 = ComponentType.TypeOf<PositionData>();
            Assert.AreNotEqual(compareType_0_0, ComponentType.Null);
            Assert.AreNotEqual(compareType_0_1, ComponentType.Null);
            Assert.AreEqual(compareType_0_0, compareType_0_1);
            Assert.AreEqual(compareType_0_0.SystemType, typeof(PositionData));
            // Compare: Generic then Type.
            var compareType_1_0 = ComponentType.TypeOf<RotationData>();
            var compareType_1_1 = ComponentType.TypeOf(typeof(RotationData));
            Assert.AreNotEqual(compareType_1_0, ComponentType.Null);
            Assert.AreNotEqual(compareType_1_1, ComponentType.Null);
            Assert.AreEqual(compareType_1_0, compareType_1_1);
            Assert.AreEqual(compareType_1_0.SystemType, typeof(RotationData));
            // Compare: not equal
            Assert.AreNotEqual(compareType_0_0, compareType_1_0);
            // Compare: 
            var compareType_2_0_0 = ComponentType.TypeOf(typeof(ScaleData));
            var compareType_2_1_0 = ComponentType.TypeOf<ManagedRendererData>();
            var compareType_2_2_0 = ComponentType.TypeOf(typeof(LayerData));
            var compareType_2_3_0 = ComponentType.TypeOf<ManagedMeshData>();
            var compareType_2_0_1 = ComponentType.TypeOf(typeof(ScaleData));
            var compareType_2_1_1 = ComponentType.TypeOf<ManagedRendererData>();
            var compareType_2_2_1 = ComponentType.TypeOf(typeof(LayerData));
            var compareType_2_3_1 = ComponentType.TypeOf<ManagedMeshData>();
            Assert.AreEqual(compareType_2_0_0, compareType_2_0_1);
            Assert.AreNotEqual(compareType_2_0_0, ComponentType.Null);
            Assert.AreEqual(compareType_2_1_0, compareType_2_1_1);
            Assert.AreNotEqual(compareType_2_1_0, ComponentType.Null);
            Assert.AreEqual(compareType_2_2_0, compareType_2_2_1);
            Assert.AreNotEqual(compareType_2_2_0, ComponentType.Null);
            Assert.AreEqual(compareType_2_3_0, compareType_2_3_1);
            Assert.AreNotEqual(compareType_2_3_0, ComponentType.Null);
            // ComponentMode:
            Assert.AreEqual(ComponentMode.Instance, compareType_2_3_1.Mode);
            var compareType_3_0_0 = ComponentType.TypeOf<TestSingleton0Data>();
            Assert.AreEqual(ComponentMode.Singleton, compareType_3_0_0.Mode);
            var compareType_3_1_0 = ComponentType.TypeOf<TestFuckingLargeSingletonData>();
            Assert.AreEqual(ComponentMode.Singleton, compareType_3_1_0.Mode);

            ComponentTypeGlobal.Dispose();
        }

        [Test]
        public void Test1_ComponentTypeGroup()
        {
            // Create/Add
            var group = ComponentTypeGroup.Combine(
                typeof(ScaleData),
                typeof(LayerData),
                typeof(RotationData),
                typeof(PositionData),
                // repeat
                typeof(LayerData));
            Assert.AreEqual(4, group.Count);
            // Contains
            var contains_0_0 = group.Contains(typeof(ScaleData));
            var contains_0_1 = group.Contains(typeof(LayerData));
            var contains_0_2 = group.Contains(typeof(RotationData));
            var contains_0_3 = group.Contains(typeof(PositionData));
            Assert.IsTrue(contains_0_0);
            Assert.IsTrue(contains_0_1);
            Assert.IsTrue(contains_0_2);
            Assert.IsTrue(contains_0_3);
            // remove
            group.Remove(typeof(ScaleData));
            Assert.AreEqual(3, group.Count);
            var contains_1_0 = group.Contains(typeof(ScaleData));
            Assert.IsFalse(contains_1_0);
            //TODO Debug UnityEngine.Debug.Log(group);

            ComponentTypeGlobal.Dispose();
        }

        [Test]
        public void Test2_EntityContainerComponentsData()
        {
            EntityContainerComponentsData chunk = default;
            var group_0 = ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData), typeof(ScaleData));
            chunk.Initialize(group_0);
            // group
            var group_1 = chunk.GetComponentGroup();
            Assert.AreEqual(group_0, group_1);
            Assert.AreEqual(group_0.Count, chunk.ComponentCount);
            // offset
            var offset_0 = chunk.OffsetOf<PositionData>();
            var offset_1 = chunk.OffsetOf(typeof(RotationData));
            var offset_2 = chunk.OffsetOf<ScaleData>();
            var offset_3 = chunk.OffsetOf(typeof(LayerData));
            Assert.AreNotEqual(-1, offset_0);
            Assert.AreNotEqual(-1, offset_1);
            Assert.AreNotEqual(-1, offset_2);
            Assert.AreEqual(-1, offset_3);
            Assert.AreNotEqual(offset_0, offset_1);
            Assert.AreNotEqual(offset_1, offset_2);
            Assert.AreNotEqual(offset_2, offset_0);
            // add
            chunk.TryAdd(typeof(LayerData), out var offset);
            Assert.AreEqual(4, chunk.ComponentCount);
            var offset_4 = chunk.OffsetOf<LayerData>();
            Assert.AreNotEqual(-1, offset_4);
            Assert.AreNotEqual(offset_3, offset_4);

            ComponentTypeGlobal.Dispose();
        }

        [Test]
        public void Test2_EntityContainerComponentsDataSingleton()
        {
            EntityContainerComponentsData chunk = default;
            if (chunk.TryAdd(typeof(TestSingleton0Data), out int offset_0)) { }
            if (chunk.TryAdd(typeof(TestSingleton1Data), out int offset_1)) { }
            if (chunk.TryAdd(typeof(TestFuckingLargeSingletonData), out int offset_2)) { }
            Assert.AreEqual(0, offset_0);
            Assert.AreEqual(16, offset_1);
            Assert.AreEqual(EntityConst.ChunkSize, offset_2);

            ComponentTypeGlobal.Dispose();
        }

        private void CompleteContainer(EntityContainer* container)
        {
            container->CompleteCreateEntities();
            container->CompleteRemoveEntities();
        }

        private struct CreateJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public EntityContainer* container;

            public void Execute(int index)
            {
                var data = container->CreateEntity();
                int innerKey = data.GetIdentity().InnerKey;
                Reference<PositionData> positionData = data.GetComponent<PositionData>();
                Reference<RotationData> rotationData = data.GetComponent<RotationData>();
                Reference<ScaleData> scaleData = data.GetComponent<ScaleData>();
                positionData.Ref = new PositionData() { position = innerKey };
                rotationData.Ref = new RotationData() { rotation = quaternion.EulerZXY(innerKey) };
                scaleData.Ref = new ScaleData() { scale = innerKey };
            }
        }

        private struct CompareJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public EntityContainer* container;

            public void Execute(int index)
            {
                var data = container->GetEntity(index);
                CompareDatas(data);
            }
        }

        private struct BatchedForeach : IJobParallelFor
        {
            public int batchCount;

            [NativeDisableUnsafePtrRestriction]
            public EntityContainer* container;

            public void Execute(int index)
            {
                int entitySize = container->entitySize;
                int begin = index * batchCount;
                int end = begin + batchCount;
                while (container->GetRange(ref begin, ref end,
                    out EntityChunk* chunk, out int chunkStartInclude,
                    out int innerStartInclude, out int innerEndExclude))
                {
                    byte* chunkData = chunk->data;
                    int innerEnd = innerEndExclude;
                    for (int i = innerStartInclude; i < innerEnd; i++)
                    {
                        int entityIndex = chunkStartInclude + i;
                        var entityData = (EntityData*)(chunkData + i * entitySize);
                        EntityComponentsData data = new EntityComponentsData(container, entityData, entityIndex);
                        CompareDatas(data);
                    }
                }
            }
        }

        private static void CompareDatas(EntityComponentsData data)
        {
            int innerKey = data.GetIdentity().InnerKey;
            Reference<PositionData> positionData = data.GetComponent<PositionData>();
            Reference<RotationData> rotationData = data.GetComponent<RotationData>();
            Reference<ScaleData> scaleData = data.GetComponent<ScaleData>();
            if (!positionData.Ref.position.Equals((float3)innerKey))
            {
                throw new System.Exception($"position not equal at {innerKey}, {(float3)innerKey} but {positionData.Ref.position}");
            }
            if (!rotationData.Ref.rotation.Equals(quaternion.EulerZXY(innerKey)))
            {
                throw new System.Exception($"rotation not equal at {innerKey}, {quaternion.EulerZXY(innerKey)} but {rotationData.Ref.rotation}");
            }
            if (!scaleData.Ref.scale.Equals((float3)innerKey))
            {
                throw new System.Exception($"scale not equal at {innerKey}, {(float3)innerKey} but {scaleData.Ref.scale}");
            }
        }

        private struct RemoveJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public EntityContainer* container;

            public void Execute(int index)
            {
                container->RemoveEntity(index);
            }
        }

        private struct CheckRecoveredInnerKeys : IJobParallelFor
        {
            public int keyStartInclude;

            public int keyEndExclude;

            [NativeDisableUnsafePtrRestriction]
            public EntityContainer* container;

            public void Execute(int index)
            {
                var entityData = container->GetEntity(index);
                int innerKey = entityData.GetIdentity().InnerKey;
                if (innerKey < keyStartInclude || innerKey >= keyEndExclude)
                {
                    throw new System.Exception($"wrong key {innerKey}.");
                }
            }
        }

        [Test]
        public void Test3_EntityContainerInstance()
        {
            EntityContainer* container = (EntityContainer*)Memory.Malloc<EntityContainer>(1, Unity.Collections.Allocator.Persistent);
            try
            {
                // Initialize
                container->Initialize(1, ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData), typeof(ScaleData)));
                Assert.AreEqual(1, container->ID);
                Assert.IsTrue(container->IsCreated);

                // frame 0 begin
                CompleteContainer(container);
                // create
                int createCount = 10000;
                // tell container to prepare chunks.
                container->WillCreate(createCount);
                CreateJob createJob = new CreateJob() { container = container };
                var createHandle = createJob.Schedule(createCount, 64);
                createHandle.Complete();
                // will complete at next frame.
                Assert.AreEqual(0, container->entityCount);

                // frame 1 begin
                // complete pre frame.
                CompleteContainer(container);
                Assert.AreEqual(createCount, container->entityCount);
                // compare values
                CompareJob compareJob = new CompareJob() { container = container };
                var compareHanlde = compareJob.Schedule(createCount, 64);
                // remove them at the same time.
                RemoveJob removeJob = new RemoveJob() { container = container };
                var removeHandle = removeJob.Schedule(createCount, 64);
                // batched foreach
                int batchRem = createCount % 64;
                int batch = (createCount / 64) + (batchRem == 0 ? 0 : 1);
                BatchedForeach batchedForeach = new BatchedForeach() { container = container, batchCount = 64 };
                var batchHandle = batchedForeach.Schedule(batch, 1);
                // combine handles.
                var frame1Handles = JobHandle.CombineDependencies(compareHanlde, removeHandle, batchHandle);
                frame1Handles.Complete();


                // frame 2 begin
                CompleteContainer(container);
                Assert.AreEqual(0, container->entityCount);
                int compareCount = 0;
                // get removed keys.
                foreach (var removedEntity in container->GetRemovedEntities())
                {
                    int belongs = removedEntity.Belongs;
                    if (belongs != 1)
                    {
                        throw new System.Exception("belongs wrong.");
                    }
                    int innerKey = removedEntity.InnerKey;
                    if (innerKey < 0 || innerKey >= createCount)
                    {
                        throw new System.Exception($"innerKey not exists: {innerKey}");
                    }
                    compareCount++;
                }
                if (compareCount != createCount)
                {
                    throw new System.Exception($"compareCount wrong, {createCount} but {compareCount}");
                }

                // frame3 begin
                CompleteContainer(container);
                // create again to test recovered innerKey
                container->WillCreate(createCount);
                createHandle = createJob.Schedule(createCount, 64);
                createHandle.Complete();
                Assert.AreEqual(0, container->entityCount);

                //  frame4 begin
                CompleteContainer(container);
                Assert.AreEqual(createCount, container->entityCount);
                // test recovered innerKey
                CheckRecoveredInnerKeys checkRecoveredInnerKeys =
                    new CheckRecoveredInnerKeys() { container = container, keyStartInclude = 0, keyEndExclude = createCount };
                var checkRecoveredHandle = checkRecoveredInnerKeys.Schedule(createCount, 64);
                checkRecoveredHandle.Complete();

                //  frame5 begin
                CompleteContainer(container);

            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                container->Dispose();
                Memory.Free(container, Unity.Collections.Allocator.Persistent);
                EntityChunkPool.Dispose();
                ComponentTypeGlobal.Dispose();
            }
        }

        [Test]
        public void Test3_EntityContainerSingleton()
        {
            EntityContainer* container = (EntityContainer*)Memory.Malloc<EntityContainer>(1, Unity.Collections.Allocator.Persistent);
            try
            {
                container->Initialize(0, ComponentTypeGroup.Null);

                // get singleton
                var singleton0Data_0 = container->GetSingleton(typeof(TestSingleton0Data));
                var singleton1Data_0 = container->GetSingleton(typeof(TestSingleton1Data));
                Assert.AreEqual(2, container->entityCount);
                Assert.AreEqual(2, container->componentsData.ComponentCount);
                var com0_0 = singleton0Data_0.GetComponent<TestSingleton0Data>();
                var com1_0 = singleton1Data_0.GetComponent<TestSingleton1Data>();
                com0_0.Ref.a = 360f;
                com1_0.Ref.a = 233f;

                var singleton0Data_1 = container->GetSingleton(typeof(TestSingleton0Data));
                var singleton1Data_1 = container->GetSingleton(typeof(TestSingleton1Data));
                Assert.AreEqual(2, container->entityCount);
                Assert.AreEqual(2, container->componentsData.ComponentCount);
                var com0_1 = singleton0Data_0.GetComponent<TestSingleton0Data>();
                var com1_1 = singleton1Data_0.GetComponent<TestSingleton1Data>();
                Assert.AreEqual(360f, com0_1.Ref.a);
                Assert.AreEqual(233f, com1_1.Ref.a);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                container->Dispose();
                Memory.Free(container, Unity.Collections.Allocator.Persistent);
                EntityChunkPool.Dispose();
                ComponentTypeGlobal.Dispose();
            }
        }

        [Test]
        public void Test4_EntityScene()
        {
            try
            {
                // static
                EntityScene scene = EntityScene.InternalGetScene();
                Assert.IsTrue(scene.IsCreated);

                // instance
                var singletons = scene.GetSingletons();
                Assert.IsTrue(singletons.IsCreated);
                var group_0 = scene.GetGroup(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData), typeof(ScaleData)));
                Assert.IsTrue(group_0.IsCreated);
                Assert.AreEqual(1, group_0.ID);
                var group_1 = scene.GetGroup(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData)));
                Assert.AreEqual(2, group_1.ID);
                var group_3 = scene.GetGroup(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData), typeof(ScaleData)));
                Assert.AreEqual(group_0.ID, group_3.ID);
                var query_0 = scene.Query(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData)));
                Assert.IsTrue(query_0.IsCreated);
            }
            finally
            {
                EntityScene.DisposeEverything();
                EntityChunkPool.Dispose();
                ComponentTypeGlobal.Dispose();
            }
        }

        private struct TestQuery_CreateJob : IJobParallelFor
        {
            public EntityGroup entityGroup;

            public void Execute(int index)
            {
                var entityData = entityGroup.Create();
                int innerKey = entityData.GetIdentity().InnerKey;
                var positionData = entityData.GetComponent<PositionData>();
                var rotationData = entityData.GetComponent<RotationData>();
                positionData.Ref = new PositionData() { position = innerKey };
                rotationData.Ref = new RotationData() { rotation = quaternion.Euler(innerKey) };
            }
        }

        private struct ForEachGroups : IEntityGroupQueryCallback
        {
            public int willCreateCount;

            public void Execute(EntityGroup group)
            {
                group.WillCreate(willCreateCount);
            }
        }

        private struct ForEachEntities : IEntityQueryCallback4
        {
            public void Execute(ref QueryResult4 result)
            {
                var entity = result.GetIdentity();
                int innerKey = entity.InnerKey;
                var positionData = result.GetComponent<PositionData>(0);
                var rotationData = result.GetComponent<RotationData>(1);
                if (!positionData.Ref.position.Equals((float3)innerKey))
                {
                    throw new System.Exception($"position not equal at {innerKey}, {(float3)innerKey} but {positionData.Ref.position}");
                }
                if (!rotationData.Ref.rotation.Equals(quaternion.Euler(innerKey)))
                {
                    throw new System.Exception($"rotation not equal at {innerKey}, {quaternion.Euler(innerKey)} but {rotationData.Ref.rotation}");
                }
            }
        }

        [Test]
        public void Test5_EntityQuery()
        {
            try
            {
                EntityScene scene = EntityScene.InternalGetScene();
                var group_0 = scene.GetGroup(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData), typeof(ScaleData)));
                var group_1 = scene.GetGroup(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData)));
                var group_2 = scene.GetGroup(ComponentTypeGroup.Combine(typeof(RotationData), typeof(ScaleData)));
                var query_0 = scene.Query(ComponentTypeGroup.Combine(typeof(PositionData), typeof(RotationData)));
                var foreachParams = QueryParams.Params(typeof(PositionData), typeof(RotationData));
                // frame 0 begin
                EntityScene.Complete().Complete();
                // create entities
                int createCount = 10000;
                // test foreachgroup
                query_0.ForEach(new ForEachGroups() { willCreateCount = createCount });
                // create jobs
                TestQuery_CreateJob createJob_0 = new TestQuery_CreateJob() { entityGroup = group_0 };
                TestQuery_CreateJob createJob_1 = new TestQuery_CreateJob() { entityGroup = group_1 };
                var createHandle_0 = createJob_0.Schedule(createCount, 64);
                var createHandle_1 = createJob_1.Schedule(createCount, 64);
                var createHandle = JobHandle.CombineDependencies(createHandle_0, createHandle_1);
                createHandle.Complete();

                // frame 1 begin
                EntityScene.Complete().Complete();
                // test foreach entities
                var foreachEntitiesHandle = query_0.ForEach(foreachParams, new ForEachEntities(), ScheduleMode.Parallel);
                foreachEntitiesHandle.Complete();
            }
            finally
            {
                EntityScene.DisposeEverything();
                QueryJobAdditionalDataMemory.Dispose();
                EntityChunkPool.Dispose();
                ComponentTypeGlobal.Dispose();
            }
        }
    }
}