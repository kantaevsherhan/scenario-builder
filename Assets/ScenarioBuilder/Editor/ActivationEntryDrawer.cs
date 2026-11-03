using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// One row of an activation list: stage badge, step, how it activates.
    /// Rows of the same stage share the badge colour, so the order can be read without the preview.
    /// </summary>
    [CustomPropertyDrawer(typeof(Activation.ActivationEntry))]
    public sealed class ActivationEntryDrawer : PropertyDrawer
    {
        private const float BadgeWidth = 62f;
        private const float ActivationWidth = 170f;
        private const float Spacing = 4f;
        private const string ArrayMarker = ".Array.data[";

        private static GUIStyle badgeStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty step = property.FindPropertyRelative("step");
            SerializedProperty activation = property.FindPropertyRelative("activation");

            var badgeRect = new Rect(position.x, position.y, BadgeWidth, position.height);
            var activationRect = new Rect(position.xMax - ActivationWidth, position.y, ActivationWidth, position.height);
            var stepRect = new Rect(badgeRect.xMax + Spacing, position.y,
                activationRect.x - badgeRect.xMax - 2 * Spacing, position.height);

            DrawBadge(badgeRect, StageOf(property));
            EditorGUI.PropertyField(stepRect, step, GUIContent.none);
            EditorGUI.PropertyField(activationRect, activation, new GUIContent(string.Empty, activation.tooltip));

            EditorGUI.EndProperty();
        }

        private static int StageOf(SerializedProperty property)
        {
            string path = property.propertyPath;
            int marker = path.LastIndexOf(ArrayMarker, System.StringComparison.Ordinal);
            if (marker < 0 || !int.TryParse(path.Substring(marker + ArrayMarker.Length).TrimEnd(']'), out int index))
            {
                return -1;
            }

            SerializedProperty list = property.serializedObject.FindProperty(path.Substring(0, marker));
            return list == null ? -1 : ScenarioEditorGUI.StageOf(list, index);
        }

        private static void DrawBadge(Rect rect, int stage)
        {
            badgeStyle ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            if (stage < 0)
            {
                GUI.Label(rect, "нет шага", badgeStyle);
                return;
            }

            Color tint = ScenarioEditorGUI.StageTint(stage);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1, 3, rect.height - 2), tint);
            EditorGUI.DrawRect(new Rect(rect.x + 3, rect.y + 1, rect.width - 3, rect.height - 2), tint * new Color(1, 1, 1, 0.18f));
            GUI.Label(rect, new GUIContent($"Этап {stage + 1}", "Шаги одного этапа активны одновременно. " +
                                                                 "Следующий этап открывается, когда завершены все шаги текущего."), badgeStyle);
        }
    }
}
