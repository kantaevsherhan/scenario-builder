using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Demo.Editor
{
    /// <summary>Assigns private serialized fields of scene components the same way the inspector does.</summary>
    internal static class SerializedWiring
    {
        public static T Set<T>(T target, string property, Object value) where T : Object
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        public static T Set<T>(T target, string property, string value) where T : Object
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        public static T Set<T>(T target, string property, bool value) where T : Object
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        public static T SetList<T>(T target, string property, IReadOnlyList<Object> values) where T : Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty(property);
            list.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }
    }
}
