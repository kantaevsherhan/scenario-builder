using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Logging;
using ScenarioBuilder.Modes;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// Runner inspector: the start mode, analyzers and loggers are picked from dropdowns of project assets.
    /// New analyzers and loggers are created next to the scenario, since the runner lives in a scene.
    /// </summary>
    [CustomEditor(typeof(ScenarioRunner))]
    public sealed class ScenarioRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var runner = (ScenarioRunner)target;

            ScenarioEditorGUI.Section("Что запускать");
            ScenarioEditorGUI.AssetPopup<ScenarioDefinition>(serializedObject.FindProperty("scenario"),
                new GUIContent("Сценарий", "Сценарий, который запускается в этой сцене."), "Не выбран");
            if (runner.Scenario != null)
            {
                ScenarioEditorGUI.AssetPopup(serializedObject.FindProperty("startMode"),
                    new GUIContent("Режим по умолчанию", "Режим, выбранный на старте. Если не задан, берётся первый режим сценария."),
                    "Первый режим сценария", runner.Scenario.Modes);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("startOnPlay"), new GUIContent("Старт без меню"));

            ScenarioEditorGUI.Section("Оценка");
            EditorGUILayout.LabelField("Между этими анализаторами можно переключаться на экране результатов. " +
                                       "Журнал при этом оценивается заново.", EditorStyles.wordWrappedMiniLabel);
            SerializedProperty analyzers = serializedObject.FindProperty("analyzers");
            EditorGUILayout.PropertyField(analyzers, new GUIContent($"Анализаторы ({analyzers.arraySize})"));
            ScenarioEditorGUI.AddAssetButton<ScenarioAnalyzer>(analyzers, "Добавить анализатор ▾", runner.Scenario);

            ScenarioEditorGUI.Section("Журнал");
            EditorGUILayout.LabelField("Журнал прохождения ведётся всегда. Логгеры дублируют его записи: " +
                                       "в консоль, в файл JSON и т. д.", EditorStyles.wordWrappedMiniLabel);
            SerializedProperty loggers = serializedObject.FindProperty("loggers");
            EditorGUILayout.PropertyField(loggers, new GUIContent($"Логгеры ({loggers.arraySize})"));
            ScenarioEditorGUI.AddAssetButton<ScenarioLogger>(loggers, "Добавить логгер ▾", runner.Scenario);

            serializedObject.ApplyModifiedProperties();

            if (runner.Scenario == null)
            {
                EditorGUILayout.HelpBox("Выберите сценарий, иначе запускать нечего.", MessageType.Warning);
            }
            else if (analyzers.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Нет ни одного анализатора: оценка будет взята только из режима.", MessageType.Info);
            }
        }
    }
}
