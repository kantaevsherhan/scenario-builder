using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Scenario inspector: step library, modes and a table of where each step is used.</summary>
    [CustomEditor(typeof(ScenarioDefinition))]
    public sealed class ScenarioDefinitionEditor : UnityEditor.Editor
    {
        private const float NumberWidth = 26f;
        private const float ModeColumnWidth = 150f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var scenario = (ScenarioDefinition)target;

            EditorGUILayout.HelpBox("Сценарий собирается из трёх частей. Шаги описывают, что сделать. " +
                                    "Режимы решают, в каком порядке шаги открываются и что стоит ошибка. " +
                                    "Шаг создаётся один раз и используется во всех режимах.", MessageType.None);

            ScenarioEditorGUI.Section("Сценарий");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), new GUIContent("Название"));

            ScenarioEditorGUI.Section("Шаги");
            SerializedProperty steps = serializedObject.FindProperty("steps");
            EditorGUILayout.PropertyField(steps, new GUIContent($"Библиотека шагов ({steps.arraySize})"));
            if (GUILayout.Button("Создать шаг", GUILayout.Width(220)))
            {
                ScenarioEditorGUI.Append(steps, ScenarioEditorGUI.CreateAssetNextTo(typeof(ActionStep), target, "Steps"));
            }

            ScenarioEditorGUI.Section("Режимы");
            SerializedProperty modes = serializedObject.FindProperty("modes");
            EditorGUILayout.PropertyField(modes, new GUIContent($"Режимы ({modes.arraySize}). Первый запускается по умолчанию"));
            ScenarioEditorGUI.AddAssetButton<ScenarioMode>(modes, "Добавить режим ▾", target);

            serializedObject.ApplyModifiedProperties();

            DrawUsageTable(scenario);
            ScenarioEditorGUI.DrawIssues(ScenarioValidator.Validate(scenario));
        }

        /// <summary>Rows are library steps, columns are modes; a cell says in which group and stage the step runs.</summary>
        private static void DrawUsageTable(ScenarioDefinition scenario)
        {
            ScenarioMode[] modes = scenario.Modes.Where(mode => mode != null).ToArray();
            if (scenario.Steps.Count == 0 || modes.Length == 0)
            {
                return;
            }

            ScenarioEditorGUI.Section("Где какой шаг");
            EditorGUILayout.LabelField("В ячейке указаны группа и этап, на котором шаг откроется. Пустая ячейка значит, что режим шаг не использует.",
                EditorStyles.wordWrappedMiniLabel);

            Rect header = EditorGUILayout.GetControlRect();
            float stepWidth = header.width - NumberWidth - modes.Length * ModeColumnWidth;
            for (int m = 0; m < modes.Length; m++)
            {
                GUI.Label(Column(header, stepWidth, m), modes[m].DisplayName, EditorStyles.miniBoldLabel);
            }

            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                StepDefinition step = scenario.Steps[i];
                Rect row = EditorGUILayout.GetControlRect();
                if (i % 2 == 0)
                {
                    EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.08f));
                }

                GUI.Label(new Rect(row.x, row.y, NumberWidth, row.height), $"{i + 1}.", EditorStyles.miniLabel);
                GUI.Label(new Rect(row.x + NumberWidth, row.y, stepWidth, row.height),
                    step != null ? new GUIContent(step.Id, step.Instruction) : new GUIContent("(пустая строка)"));

                for (int m = 0; m < modes.Length; m++)
                {
                    GUI.Label(Column(row, stepWidth, m), step != null ? Placement(modes[m], step) : string.Empty, EditorStyles.miniLabel);
                }
            }
        }

        private static Rect Column(Rect row, float stepWidth, int index) =>
            new(row.x + NumberWidth + stepWidth + index * ModeColumnWidth, row.y, ModeColumnWidth, row.height);

        private static string Placement(ScenarioMode mode, StepDefinition step)
        {
            for (int g = 0; g < mode.Groups.Count; g++)
            {
                var stages = ActivationPlan.BuildStages(mode.Groups[g].Steps);
                int stage = stages.FindIndex(items => items.Contains(step));
                if (stage >= 0)
                {
                    return mode.Groups.Count > 1 ? $"группа {g + 1}, этап {stage + 1}" : $"этап {stage + 1}";
                }
            }

            return string.Empty;
        }
    }
}
