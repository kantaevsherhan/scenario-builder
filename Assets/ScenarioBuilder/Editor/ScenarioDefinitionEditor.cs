using ScenarioBuilder.Core;
using UnityEditor;

namespace ScenarioBuilder.Editor
{
    /// <summary>Scenario inspector that also validates the steps and every mode.</summary>
    [CustomEditor(typeof(ScenarioDefinition))]
    public sealed class ScenarioDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ScenarioEditorGUI.DrawIssues(ScenarioValidator.Validate((ScenarioDefinition)target));
        }
    }
}
