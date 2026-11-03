using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Step inspector: validation plus the list of modes and groups that reuse this step.</summary>
    [CustomEditor(typeof(StepDefinition), true)]
    public sealed class StepDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var step = (StepDefinition)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Где используется", EditorStyles.boldLabel);

            List<string> usages = FindUsages(step);
            if (usages.Count == 0)
            {
                EditorGUILayout.HelpBox("Шаг не используется ни в одном режиме.", MessageType.Info);
            }

            foreach (string usage in usages)
            {
                EditorGUILayout.LabelField("•", usage);
            }

            ScenarioEditorGUI.DrawIssues(step.Validate().ToList());
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
