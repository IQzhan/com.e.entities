using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace E.Entities
{
    public class ManagedRendererDataConverter : IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to)
        {
            from = typeof(MeshRenderer);
            to = ComponentType.TypeOf<ManagedRendererData>();
        }

        public unsafe void CopyValue(Component unityComponent, IntPtr data)
        {
            MeshRenderer meshRenderer = unityComponent as MeshRenderer;
            Material material = meshRenderer.sharedMaterial;
            ShadowCastingMode shadowCastingMode = meshRenderer.shadowCastingMode;
            bool receiveShadows = meshRenderer.receiveShadows;
            *(ManagedRendererData*)(data) = new ManagedRendererData()
            {
                material = material != null ? ClassReference.Register(material) : default,
                CastShadows = shadowCastingMode,
                ReceiveShadows = receiveShadows
            };
        }
    }
}