using System;
using Unity.Mathematics;
using UnityEngine;

namespace E.Entities
{
    public class PositionDataConverter : IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to)
        {
            from = typeof(Transform);
            to = ComponentType.TypeOf<PositionData>();
        }

        public unsafe void CopyValue(Component unityComponent, IntPtr data)
        {
            var transform = unityComponent as Transform;
            float3 position = transform.position;
            *(PositionData*)data = new PositionData() { position = position };
        }
    }
}