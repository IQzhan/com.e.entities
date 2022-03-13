using UnityEngine;

namespace E.Entities
{
    public struct ManagedMeshData : IComponent
    {
        public ClassReference<Mesh> mesh;
    }
}