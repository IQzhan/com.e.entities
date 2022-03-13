using E.Entities;
using Unity.Jobs;
using UnityEngine;

namespace E
{
    public class Test0_System : IEntitySystem
    {
        public void Start(ref JobHandle dependsOn)
        {
            Debug.Log("Start0");
        }

        public void Update(ref JobHandle dependsOn)
        {
            
        }

        public void Dispose()
        {
            Debug.Log("Dispose0");
        }
    }
}