using ScenarioBuilder.Steps;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Shared layout for the step rule rows: plain fields with Russian labels, no nested foldouts.</summary>
    internal static class RuleRows
    {
        public static float Line => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public static Rect Row(Rect position, int index) =>
            new(position.x, position.y + index * Line, position.width, EditorGUIUtility.singleLineHeight);

        public static void Field(Rect rect, SerializedProperty property, string label) =>
            EditorGUI.PropertyField(rect, property, new GUIContent(label, property.tooltip));
    }

    [CustomPropertyDrawer(typeof(RequiredAction))]
    public sealed class RequiredActionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            RuleRows.Line + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requirements"), true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty requirements = property.FindPropertyRelative("requirements");
            RuleRows.Field(RuleRows.Row(position, 0), property.FindPropertyRelative("action"), "Действие");

            var rest = new Rect(position.x, position.y + RuleRows.Line, position.width, position.height - RuleRows.Line);
            EditorGUI.PropertyField(rest, requirements,
                new GUIContent($"Условия в момент действия ({requirements.arraySize})", requirements.tooltip), true);
        }
    }

    [CustomPropertyDrawer(typeof(StateRequirement))]
    public sealed class StateRequirementDrawer : PropertyDrawer
    {
        private static readonly GUIContent[] States = { new("должно быть выключено"), new("должно быть включено") };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => RuleRows.Line * 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mustBeSet = property.FindPropertyRelative("mustBeSet");
            Rect first = RuleRows.Row(position, 0);
            const float stateWidth = 180f;

            RuleRows.Field(new Rect(first.x, first.y, first.width - stateWidth - 4, first.height),
                property.FindPropertyRelative("flag"), "Состояние");
            int state = EditorGUI.Popup(new Rect(first.xMax - stateWidth, first.y, stateWidth, first.height),
                mustBeSet.boolValue ? 1 : 0, States);
            mustBeSet.boolValue = state == 1;

            RuleRows.Field(RuleRows.Row(position, 1), property.FindPropertyRelative("violationMessage"), "Текст нарушения");
        }
    }

    [CustomPropertyDrawer(typeof(ForbiddenAction))]
    public sealed class ForbiddenActionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => RuleRows.Line * 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RuleRows.Field(RuleRows.Row(position, 0), property.FindPropertyRelative("action"), "Действие");
            RuleRows.Field(RuleRows.Row(position, 1), property.FindPropertyRelative("message"), "Текст нарушения");
        }
    }
}
