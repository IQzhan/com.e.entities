using System;
using Unity.Mathematics;
using UnityEngine;

namespace E.Entities
{
    public class RotationDataConverter : IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to)
        {
            from = typeof(Transform);
            to = ComponentType.TypeOf<RotationData>();
        }

        public unsafe void CopyValue(Component unityComponent, IntPtr data)
        {
            var transform = unityComponent as Transform;
            quaternion rotation = transform.rotation;
            *(RotationData*)data = new RotationData() { rotation = rotation };
        }
    }
}