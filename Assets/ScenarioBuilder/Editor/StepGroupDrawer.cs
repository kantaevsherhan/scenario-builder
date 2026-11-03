using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Exam group: the header says how many steps it has and how they open, the body is name and steps.</summary>
    [CustomPropertyDrawer(typeof(StepGroup))]
    public sealed class StepGroupDrawer : PropertyDrawer
    {
        private static GUIStyle headerStyle;

        private static GUIStyle HeaderStyle => headerStyle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            property.isExpanded
                ? RuleRows.Line * 2 + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("steps"), true)
                : RuleRows.Line;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty name = property.FindPropertyRelative("name");
            SerializedProperty steps = property.FindPropertyRelative("steps");

            string title = string.IsNullOrEmpty(name.stringValue) ? label.text : name.stringValue;
            property.isExpanded = EditorGUI.Foldout(RuleRows.Row(position, 0), property.isExpanded,
                $"{title}  ·  шагов: {steps.arraySize}, {ScenarioEditorGUI.OrderKind(StageSizes(steps))}", true, HeaderStyle);
            if (!property.isExpanded)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                RuleRows.Field(RuleRows.Row(position, 1), name, "Название группы");
                var list = new Rect(position.x, position.y + RuleRows.Line * 2, position.width, position.height - RuleRows.Line * 2);
                EditorGUI.PropertyField(list, steps, new GUIContent("Шаги по порядку", steps.tooltip), true);
            }
        }

        private static List<int> StageSizes(SerializedProperty steps) =>
            Enumerable.Range(0, steps.arraySize)
                .Select(i => ScenarioEditorGUI.StageOf(steps, i))
                .Where(stage => stage >= 0)
                .GroupBy(stage => stage)
                .Select(stage => stage.Count())
                .ToList();
    }
}
