using UnityEngine;

namespace E.Entities
{
    [AddComponentMenu("")]
    internal class EntityLifeCycleBehaviour : MonoBehaviour
    {
        private void Update()
        {
            EntityLifeCycle.instance.Execute();
        }
    }
}