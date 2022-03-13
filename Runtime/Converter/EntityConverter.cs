using E.Collections.Unsafe;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace E.Entities
{
    public struct EntityConvertParams
    {
        internal EntityConvertParams(
            ref ComponentTypeGroup group,
            ComponentTypeOffset[] offsets,
            Component[] unityComponents,
            EntityGameObjectIncluded gameObjectIncluded)
        {
            this.group = group;
            this.offsets = offsets;
            this.unityComponents = unityComponents;
            this.gameObjectIncluded = gameObjectIncluded;
        }

        internal struct ComponentTypeOffset
        {
            public short ID;
            public short offset;
        }

        public bool IsCreated => offsets != null;

        public readonly ComponentTypeGroup group;

        internal readonly ComponentTypeOffset[] offsets;

        internal readonly Component[] unityComponents;

        internal readonly EntityGameObjectIncluded gameObjectIncluded;

        internal int GetOffset(int id)
        {
            int low = 0;
            int high = group.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                ComponentTypeOffset componentOffset = offsets[mid];
                int midValue = componentOffset.ID;
                int compare = midValue - id;
                if (compare < 0)
                {
                    // in right
                    low = mid + 1;
                }
                else if (compare > 0)
                {
                    // in left
                    high = mid - 1;
                }
                else
                {
                    // match
                    return componentOffset.offset;
                }
            }
            return -1;
        }
    }

    public sealed unsafe class EntityConverter
    {
        private static EntityConverter m_instance;

        private Dictionary<Type, IEntityComponentConverter[]> m_Converters;

        static EntityConverter()
        {
            m_instance = new EntityConverter();
            m_instance.Collect();
        }

        /// <summary>
        /// Convert a gameObject to entity.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="gameObjectIncluded"></param>
        /// <returns></returns>
        public static EntityComponentsData Convert(GameObject gameObject, EntityGameObjectIncluded gameObjectIncluded)
        {
            if (!Application.isPlaying)
            {
                throw new Exception("Convert to entity only allowed in play mode.");
            }
            var convertParams = m_instance.InternalCreateEntityConvertParams(gameObject, gameObjectIncluded);
            var scene = EntityScene.GetScene();
            var group = scene.GetGroup(convertParams.group);
            group.WillCreate(1);
            var entityData = group.Create();
            m_instance.InternalConvertToUnmanagedData(convertParams, entityData.GetComponentsPtr());
            return entityData;
        }

        /// <summary>
        /// Create params for ConvertToUnmanagedData()
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="gameObjectIncluded"></param>
        /// <returns></returns>
        public static EntityConvertParams CreateEntityConvertParams(GameObject gameObject, EntityGameObjectIncluded gameObjectIncluded)
        {
            if (!Application.isPlaying)
            {
                throw new Exception("Convert only allowed in play mode.");
            }
            return m_instance.InternalCreateEntityConvertParams(gameObject, gameObjectIncluded);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="convertParams"></param>
        /// <param name="data"></param>
        public static void ConvertToUnmanagedData(EntityConvertParams convertParams, IntPtr data)
        {
            if (!Application.isPlaying)
            {
                throw new Exception("Convert only allowed in play mode.");
            }
            m_instance.InternalConvertToUnmanagedData(convertParams, data);
        }

        /// <summary>
        /// Collect all converters.
        /// </summary>
        private void Collect()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
            var baseType = typeof(IEntityComponentConverter);
            var unityComponentType = typeof(Component);
            var allConverterTypes = allTypes
                .Where(t => baseType.IsAssignableFrom(t) &&
                t.IsClass &&
                !t.IsAbstract &&
                !t.IsGenericType)
                .Select(t => Activator.CreateInstance(t) as IEntityComponentConverter)
                .GroupBy(c =>
                {
                    c.GetConvertType(out var from, out var to);
                    if (!unityComponentType.IsAssignableFrom(from))
                    {
                        throw new ArgumentException($"{c.GetType()}.GetConvertType(out Type [from], out ComponentType to).", "from");
                    }
                    if (to == ComponentType.Null)
                    {
                        throw new ArgumentException($"{c.GetType()}.GetConvertType(out Type from, out ComponentType [to]).", "to");
                    }
                    return from;
                },
                (t, a) => new KeyValuePair<Type, IEntityComponentConverter[]>(t, a.ToArray()));
            m_Converters = new Dictionary<Type, IEntityComponentConverter[]>(allConverterTypes);
        }

        private EntityConvertParams InternalCreateEntityConvertParams(GameObject gameObject, EntityGameObjectIncluded gameObjectIncluded)
        {
            Component[] unityComponents = gameObject.GetComponents<Component>();
            ComponentTypeGroup componentTypes = default;
            var positionDataType = ComponentType.TypeOf<PositionData>();
            var rotationDataType = ComponentType.TypeOf<RotationData>();
            var scaleDataType = ComponentType.TypeOf<ScaleData>();
            var layerDataType = ComponentType.TypeOf<LayerData>();
            foreach (Component unityComponent in unityComponents)
            {
                if (unityComponent is ConvertToEntity) continue;
                Type unityComponentType = unityComponent.GetType();
                if (!m_Converters.TryGetValue(unityComponentType, out var converters))
                {
                    Debug.LogWarning($"Can not convert unity component {unityComponentType} to entity component. No implement founded.");
                    continue;
                }
                bool isTransform = unityComponent is Transform;
                foreach (IEntityComponentConverter converter in converters)
                {
                    converter.GetConvertType(out var _, out var to);
                    if ((isTransform && (
                        ((to == positionDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Position) != 0)) ||
                        ((to == rotationDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Rotation) != 0)) ||
                        ((to == scaleDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Scale) != 0)) ||
                        ((to == layerDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Layer) != 0))))
                        || !isTransform)
                    {
                        componentTypes.CombineWith(to);
                    }
                }
            }
            if (componentTypes.Count > 0)
            {
                EntityConvertParams.ComponentTypeOffset[] offsets = new EntityConvertParams.ComponentTypeOffset[componentTypes.Count];
                short offset = 0;
                int i = 0;
                foreach (var componentTye in componentTypes)
                {
                    offsets[i++] = new EntityConvertParams.ComponentTypeOffset()
                    {
                        ID = componentTye.ID,
                        offset = offset
                    };
                    offset += componentTye.Size;
                }
                return new EntityConvertParams(ref componentTypes, offsets, unityComponents, gameObjectIncluded);
            }
            else
            {
                throw new Exception("Nothing to be converted.");
            }
        }

        private void InternalConvertToUnmanagedData(EntityConvertParams convertParams, IntPtr data)
        {
            if (!convertParams.IsCreated)
            {
                throw new ArgumentException("Not created.", "convertParams");
            }
            Memory.Clear((void*)data, convertParams.group.Size);
            var positionDataType = ComponentType.TypeOf<PositionData>();
            var rotationDataType = ComponentType.TypeOf<RotationData>();
            var scaleDataType = ComponentType.TypeOf<ScaleData>();
            var layerDataType = ComponentType.TypeOf<LayerData>();
            var gameObjectIncluded = convertParams.gameObjectIncluded;
            foreach (Component unityComponent in convertParams.unityComponents)
            {
                Type unityComponentType = unityComponent.GetType();
                if (!m_Converters.TryGetValue(unityComponentType, out var converters))
                {
                    continue;
                }
                bool isTransform = unityComponent is Transform;
                foreach (IEntityComponentConverter converter in converters)
                {
                    converter.GetConvertType(out var _, out var to);
                    if ((isTransform && (
                        ((to == positionDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Position) != 0)) ||
                        ((to == rotationDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Rotation) != 0)) ||
                        ((to == scaleDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Scale) != 0)) ||
                        ((to == layerDataType) && ((gameObjectIncluded & EntityGameObjectIncluded.Layer) != 0))))
                        || !isTransform)
                    {
                        int offset = convertParams.GetOffset(to.ID);
                        IntPtr component = (IntPtr)(((byte*)data) + offset);
                        converter.CopyValue(unityComponent, component);
                    }
                }
            }
        }
    }
}