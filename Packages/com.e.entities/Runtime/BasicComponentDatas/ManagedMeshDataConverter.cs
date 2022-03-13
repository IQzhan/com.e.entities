using System;
using UnityEngine;

namespace E.Entities
{
    public class ManagedMeshDataConverter : IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to)
        {
            from = typeof(MeshFilter);
            to = ComponentType.TypeOf<ManagedMeshData>();
        }

        public unsafe void CopyValue(Component unityComponent, IntPtr data)
        {
            var meshFilter = unityComponent as MeshFilter;
            Mesh mesh = meshFilter.sharedMesh;
            *(ManagedMeshData*)data = new ManagedMeshData() { mesh = mesh != null ? ClassReference.Register(mesh) : default };
        }
    }
}