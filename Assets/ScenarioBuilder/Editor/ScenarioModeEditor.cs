using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// Mode inspector in the order a mode is put together: about, rules, scoring, steps,
    /// then a preview of the resulting stages and the configuration check.
    /// </summary>
    [CustomEditor(typeof(ScenarioMode), true)]
    public sealed class ScenarioModeEditor : UnityEditor.Editor
    {
        private static readonly HashSet<string> LaidOutFields = new()
        {
            "m_Script", "displayName", "description", "showHints", "outOfTurnPolicy", "defaultAnalyzer",
            "violationThreshold", "steps", "groups",
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var mode = (ScenarioMode)target;
            ScenarioDefinition scenario = ScenarioEditorGUI.FindScenarioOf(mode);

            EditorGUILayout.HelpBox(About(mode), MessageType.None);

            ScenarioEditorGUI.Section("Режим");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), new GUIContent("Название"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("Описание"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showHints"), new GUIContent("Подсказки и тур"));

            ScenarioEditorGUI.Section("Правила");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outOfTurnPolicy"), new GUIContent("Действие не в свою очередь"));
            SerializedProperty threshold = serializedObject.FindProperty("violationThreshold");
            if (threshold != null)
            {
                EditorGUILayout.PropertyField(threshold, new GUIContent("Порог нарушений в группе"));
                EditorGUILayout.HelpBox($"Каждое нарушение проваливает шаг. На {threshold.intValue}-м нарушении в группе " +
                                        "её незавершённые шаги пропускаются и начинается следующая группа.", MessageType.None);
            }
            else if (mode is TrainingMode)
            {
                EditorGUILayout.HelpBox("Нарушение не завершает шаг: ошибка записывается в журнал, и шаг можно повторить.", MessageType.None);
            }

            ScenarioEditorGUI.Section("Оценка");
            ScenarioEditorGUI.AssetPopup<ScenarioAnalyzer>(serializedObject.FindProperty("defaultAnalyzer"),
                new GUIContent("Анализатор", "Чем оценивается прохождение в этом режиме. На экране результатов можно переключиться на другой."),
                "Не задан (первый из ScenarioRunner)");

            DrawSteps(scenario);
            DrawOtherFields();
            serializedObject.ApplyModifiedProperties();

            DrawPreview(mode, scenario);
            ScenarioEditorGUI.DrawIssues(scenario != null
                ? ScenarioValidator.Validate(mode, scenario.Steps.Where(step => step != null).ToList())
                : ScenarioValidator.Validate(mode));
        }

        private static string About(ScenarioMode mode) => mode switch
        {
            TrainingMode => "Тренировка: все шаги в одном списке, ошибку можно исправить и повторить шаг.",
            ExamMode => "Экзамен: шаги разбиты на группы, которые идут по очереди. Ошибка проваливает шаг, " +
                        "а после порога нарушений группа закрывается.",
            _ => "Режим прохождения: в каком порядке открываются шаги и что стоит ошибка.",
        };

        private void DrawSteps(ScenarioDefinition scenario)
        {
            SerializedProperty steps = serializedObject.FindProperty("steps");
            SerializedProperty groups = serializedObject.FindProperty("groups");

            if (steps != null)
            {
                ScenarioEditorGUI.Section("Шаги");
                DrawOrderHint();
                EditorGUILayout.PropertyField(steps, new GUIContent($"Порядок активации ({steps.arraySize})"));
                ScenarioEditorGUI.AddStepButton(steps, scenario, "Добавить шаг сценария ▾");
            }

            if (groups != null)
            {
                ScenarioEditorGUI.Section("Группы");
                DrawOrderHint();
                EditorGUILayout.PropertyField(groups, new GUIContent($"Группы по порядку ({groups.arraySize})"));
                for (int i = 0; i < groups.arraySize; i++)
                {
                    SerializedProperty group = groups.GetArrayElementAtIndex(i);
                    string name = group.FindPropertyRelative("name").stringValue;
                    ScenarioEditorGUI.AddStepButton(group.FindPropertyRelative("steps"), scenario,
                        $"Добавить шаг в «{(string.IsNullOrEmpty(name) ? $"группу {i + 1}" : name)}» ▾");
                }
            }
        }

        private static void DrawOrderHint()
        {
            EditorGUILayout.HelpBox("Строки перетаскиваются за левый край. «После предыдущих» открывает новый этап, " +
                                    "«Вместе с предыдущим» добавляет шаг в этап строкой выше. " +
                                    "Шаги одного этапа можно выполнять в любом порядке.", MessageType.None);
        }

        /// <summary>Fields added by custom mode classes.</summary>
        private void DrawOtherFields()
        {
            SerializedProperty property = serializedObject.GetIterator();
            bool first = true;
            for (bool enter = true; property.NextVisible(enter); enter = false)
            {
                if (LaidOutFields.Contains(property.name))
                {
                    continue;
                }

                if (first)
                {
                    ScenarioEditorGUI.Section("Другие настройки");
                    first = false;
                }

                EditorGUILayout.PropertyField(property, true);
            }
        }

        private static void DrawPreview(ScenarioMode mode, ScenarioDefinition scenario)
        {
            ScenarioEditorGUI.Section("Как пойдут шаги");
            if (scenario != null)
            {
                EditorGUILayout.LabelField("Цифры обозначают номера шагов в сценарии «" + scenario.Title + "», стрелка ведёт к следующему этапу.",
                    EditorStyles.wordWrappedMiniLabel);
            }

            IReadOnlyList<StepGroup> groups = mode.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                StepGroup group = groups[i];
                string header = groups.Count > 1 ? $"{i + 1}. {group.Name}" : group.Name;
                EditorGUILayout.LabelField(header, ScenarioEditorGUI.OrderKind(group.Steps), EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawStages(group, scenario);
                }
            }
        }

        private static void DrawStages(StepGroup group, ScenarioDefinition scenario)
        {
            List<List<Steps.StepDefinition>> stages = ActivationPlan.BuildStages(group.Steps);
            if (stages.Count == 0)
            {
                EditorGUILayout.LabelField("Нет шагов.");
                return;
            }

            EditorGUILayout.LabelField(ScenarioEditorGUI.StageChain(group.Steps, scenario), EditorStyles.boldLabel);
            for (int i = 0; i < stages.Count; i++)
            {
                string together = stages[i].Count > 1 ? " · вместе" : string.Empty;
                string steps = string.Join(", ", stages[i].Select(step =>
                    scenario != null ? $"{ScenarioEditorGUI.StepNumber(step, scenario)}. {step.Id}" : step.Id));
                EditorGUILayout.LabelField($"Этап {i + 1}{together}", steps, EditorStyles.wordWrappedLabel);

                Rect row = EditorGUI.IndentedRect(GUILayoutUtility.GetLastRect());
                EditorGUI.DrawRect(new Rect(row.x - 6, row.y + 2, 3, row.height - 4), ScenarioEditorGUI.StageTint(i));
            }
        }
    }
}
