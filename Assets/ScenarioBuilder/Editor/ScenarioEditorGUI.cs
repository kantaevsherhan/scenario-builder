using System.Collections.Generic;
using UnityEditor;

namespace ScenarioBuilder.Editor
{
    internal static class ScenarioEditorGUI
    {
        public static void DrawIssues(IReadOnlyCollection<string> issues)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Проверка настроек", EditorStyles.boldLabel);
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Ошибок не найдено.", MessageType.Info);
                return;
            }

            foreach (string issue in issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }
    }
}
