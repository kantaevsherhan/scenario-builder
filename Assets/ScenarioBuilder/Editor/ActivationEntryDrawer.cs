using ScenarioBuilder.Activation;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    [CustomPropertyDrawer(typeof(ActivationEntry))]
    public sealed class ActivationEntryDrawer : PropertyDrawer
    {
        private const float ActivationWidth = 175f;
        private const float Spacing = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty step = property.FindPropertyRelative("step");
            SerializedProperty activation = property.FindPropertyRelative("activation");

            var stepRect = new Rect(position.x, position.y, position.width - ActivationWidth - Spacing, position.height);
            var activationRect = new Rect(position.xMax - ActivationWidth, position.y, ActivationWidth, position.height);

            EditorGUI.PropertyField(stepRect, step, GUIContent.none);
            EditorGUI.PropertyField(activationRect, activation, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
