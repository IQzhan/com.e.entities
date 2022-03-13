using System;
using UnityEngine;

namespace E.Entities
{
    public class LayerDataConverter : IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to)
        {
            from = typeof(Transform);
            to = ComponentType.TypeOf<LayerData>();
        }

        public unsafe void CopyValue(Component unityComponent, IntPtr data)
        {
            int layer = unityComponent.gameObject.layer;
            ((LayerData*)data)->low = layer;
        }
    }
}