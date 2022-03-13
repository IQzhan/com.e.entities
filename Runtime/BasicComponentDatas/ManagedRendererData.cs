using UnityEngine;
using UnityEngine.Rendering;

namespace E.Entities
{
    public struct ManagedRendererData : IComponent
    {
        public ClassReference<Material> material;

        private uint m_Mask;

        public ShadowCastingMode CastShadows
        {
            get => (ShadowCastingMode)(m_Mask & 0b11);
            set => m_Mask = (m_Mask & 0b100) | (uint)value;
        }

        public bool ReceiveShadows
        {
            get => (m_Mask >> 2) == 1;
            set => m_Mask = (m_Mask & 0b011) | (value ? 0b100u : 0b000u);
        }
    }
}