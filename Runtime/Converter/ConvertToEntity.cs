using UnityEngine;

namespace E.Entities
{
    public unsafe class ConvertToEntity : MonoBehaviour
    {
        public enum ConvertMode
        {
            ConvertThenDestroy,
            ConvertOnly
        }

        public ConvertMode convertMode = ConvertMode.ConvertThenDestroy;

        public EntityGameObjectIncluded basicIncluded = EntityGameObjectIncluded.RigidWithLayer;

        private void Start()
        {
            Convert();
        }

        private void Convert()
        {
            EntityComponentsData componentsData = EntityConverter.Convert(gameObject, basicIncluded);
            if (componentsData.IsCreated && convertMode == ConvertMode.ConvertThenDestroy)
            {
                Destroy(gameObject);
            }
        }
    }
}