using System;
using UnityEngine;

namespace E.Entities
{
    public interface IEntityComponentConverter
    {
        public void GetConvertType(out Type from, out ComponentType to);

        public void CopyValue(Component unityComponent, IntPtr data);
    }
}