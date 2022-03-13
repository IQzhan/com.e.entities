using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace E.Entities
{
    public struct PositionData : IComponent
    {
        public float3 position;
    }
}