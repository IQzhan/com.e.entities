using System;
using Unity.Mathematics;
using UnityEngine;

namespace E.Entities
{
    public class ScaleDataConverter : IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to)
        {
            from = typeof(Transform);
            to = ComponentType.TypeOf<ScaleData>();
        }

        public unsafe void CopyValue(Component unityComponent, IntPtr data)
        {
            var transform = unityComponent as Transform;
            float3 scale = transform.lossyScale;
            *(ScaleData*)data = new ScaleData() { scale = scale };
        }
    }
}