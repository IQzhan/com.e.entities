using System;
using Unity.Jobs;

namespace E.Entities
{
    public interface IEntitySystem : IDisposable
    {
        public void Start(ref JobHandle dependsOn);

        public void Update(ref JobHandle dependsOn);
    }
}