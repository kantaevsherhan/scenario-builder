using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Step inspector: the step's own fields, the modes and groups that reuse it, validation.</summary>
    [CustomEditor(typeof(StepDefinition), true)]
    public sealed class StepDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target is ActionStep)
            {
                DrawActionStep();
            }
            else
            {
                DrawDefaultInspector();
            }

            var step = (StepDefinition)target;
            ScenarioEditorGUI.Section("Где используется");

            List<string> usages = FindUsages(step);
            if (usages.Count == 0)
            {
                EditorGUILayout.HelpBox("Шаг не используется ни в одном режиме.", MessageType.Info);
            }

            foreach (string usage in usages)
            {
                EditorGUILayout.LabelField($"• {usage}");
            }

            ScenarioEditorGUI.DrawIssues(step.Validate().ToList());
        }

        private void DrawActionStep()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Шаг завершается, когда действия из списка выполнены по порядку. " +
                                    "У действия можно задать условие, например «Клапан открыт». " +
                                    "Запрещённое действие во время шага считается ошибкой. Когда шаг откроется, решает режим.",
                MessageType.None);

            ScenarioEditorGUI.Section("Шаг");
            Field("id", "Идентификатор");
            Field("instruction", "Текст задания");

            ScenarioEditorGUI.Section("Что сделать");
            SerializedProperty required = serializedObject.FindProperty("requiredActions");
            Field("requiredActions", $"Действия по порядку ({required.arraySize})");
            Field("wrongOrderIsViolation", "Не по порядку = нарушение");

            ScenarioEditorGUI.Section("Чего не делать");
            SerializedProperty forbidden = serializedObject.FindProperty("forbiddenActions");
            Field("forbiddenActions", $"Запрещённые действия ({forbidden.arraySize})");
            serializedObject.ApplyModifiedProperties();
        }

        private void Field(string name, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip), true);
        }

        private static List<string> FindUsages(StepDefinition step)
        {
            var usages = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(ScenarioMode)}"))
            {
                var mode = AssetDatabase.LoadAssetAtPath<ScenarioMode>(AssetDatabase.GUIDToAssetPath(guid));
                if (mode == null)
                {
                    continue;
                }

                foreach (StepGroup group in mode.Groups.Where(group => group.Steps.Any(entry => entry?.Step == step)))
                {
                    usages.Add(mode.Groups.Count > 1 ? $"{mode.DisplayName} → {group.Name}" : mode.DisplayName);
                }
            }

            return usages;
        }
    }
}
