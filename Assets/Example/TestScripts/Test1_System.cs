using E.Collections.Unsafe;
using E.Entities;
using System;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static E.Entities.Static;

namespace E
{
    public unsafe class Test1_System : IEntitySystem
    {
        private bool m_Loaded;

        private PrefabReferences prefabReferences;

        private EntityQuery m_RigidQuery;

        private readonly int xLength = 10;

        private readonly int zLength = 10;

        private float interval = 0.2f;

        // for show result
        private Mesh theMesh;

        private Material theMaterial;

        private Matrix4x4[] matrix4X4s;

        public void Start(ref JobHandle dependsOn)
        {
            Debug.Log("Start test1 system");
            // get prefab
            prefabReferences = UnityEngine.Object.FindObjectOfType<PrefabReferences>();
            if (prefabReferences == null) return;
            var prefab0 = prefabReferences.prefab0;
            if (prefab0 == null) return;
            // create instance
            var convertParams = EntityConverter.CreateEntityConvertParams(prefab0, EntityGameObjectIncluded.RigidWithLayer);
            var size = convertParams.group.Size;
            byte* comData = stackalloc byte[size];
            EntityConverter.ConvertToUnmanagedData(convertParams, (IntPtr)comData);
            // use comData to create
            EntityScene entityScene = GetScene();
            var entityGroup = entityScene.GetGroup(convertParams.group);
            // create
            int count = xLength * zLength;
            entityGroup.WillCreate(count);
            for (int i = 0; i < count; i++)
            {
                var createdData = entityGroup.Create();
                var comPtr = createdData.GetComponentsPtr();
                Memory.Copy((void*)comPtr, comData, size);
            }
            // create query
            m_RigidQuery = entityScene.Query(convertParams.group);

            // for show result
            theMesh = prefab0.GetComponent<MeshFilter>().sharedMesh;
            theMaterial = prefab0.GetComponent<MeshRenderer>().sharedMaterial;
            matrix4X4s = new Matrix4x4[count];

            m_Loaded = true;
        }

        struct MoveCallback : IEntityQueryCallback
        {
            public float time;

            public int xLength;

            public int zLength;

            public float interval;

            public void Execute(ref QueryResult result)
            {
                int index = result.GetIdentity().LatestIndex;
                float x = index % xLength;
                float z = index / zLength;
                float dist = 0.3f * Vector2.Distance(new Vector2(x, z), new Vector2(xLength * 0.5f, zLength * 0.5f));
                float y = (float)Math.Sin(time + dist);
                Vector3 target = new Vector3(x + x * interval, y, z + z * interval);
                result.GetComponent<PositionData>(0).Ref.position = target;
            }
        }

        struct CopyMartixs : IEntityQueryCallback
        {
            public Matrix4x4[] matrix4X4s;

            public void Execute(ref QueryResult result)
            {
                var pos = result.GetComponent<PositionData>(0).Ref.position;
                matrix4X4s[result.GetIdentity().LatestIndex] = float4x4.Translate(pos);
            }
        }

        public void Update(ref JobHandle dependsOn)
        {
            if (!m_Loaded || !prefabReferences.enableUpdate) return;

            // update
            dependsOn = m_RigidQuery.ForEach(Params(TypeOf<PositionData>()),
                new MoveCallback() { interval = interval, xLength = xLength, zLength = zLength, time = Time.time },
                ScheduleMode.Parallel, dependsOn);

            // show result
            dependsOn = m_RigidQuery.ForEach(Params(TypeOf<PositionData>()),
                new CopyMartixs() { matrix4X4s = matrix4X4s },
                ScheduleMode.Run, dependsOn);
            Graphics.DrawMeshInstanced(theMesh, 0, theMaterial, matrix4X4s);
        }

        public void Dispose()
        {
            Debug.Log("Dispose test1 system");
        }
    }
}