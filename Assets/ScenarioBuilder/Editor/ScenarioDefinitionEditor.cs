using ScenarioBuilder.Core;
using UnityEditor;

namespace ScenarioBuilder.Editor
{
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
