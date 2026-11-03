using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;

namespace ScenarioBuilder.Editor
{
    /// <summary>Mode inspector with a readable preview of the resulting activation stages.</summary>
    [CustomEditor(typeof(ScenarioMode), true)]
    public sealed class ScenarioModeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mode = (ScenarioMode)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Порядок активации", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Шаги одного этапа активны одновременно и выполняются в любом порядке. " +
                                    "Следующий этап открывается, когда завершены все шаги текущего.", MessageType.None);

            foreach (StepGroup group in mode.Groups)
            {
                DrawGroupPreview(group);
            }

            ScenarioEditorGUI.DrawIssues(ScenarioValidator.Validate(mode));
        }

        private static void DrawGroupPreview(StepGroup group)
        {
            EditorGUILayout.LabelField(group.Name, EditorStyles.miniBoldLabel);
            using var indent = new EditorGUI.IndentLevelScope();

            List<List<StepDefinition>> stages = ActivationPlan.BuildStages(group.Steps);
            if (stages.Count == 0)
            {
                EditorGUILayout.LabelField("— нет шагов —");
                return;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                string steps = string.Join(", ", stages[i].Select(step => step.Id));
                EditorGUILayout.LabelField($"Этап {i + 1}", steps, EditorStyles.wordWrappedLabel);
            }
        }
    }
}
