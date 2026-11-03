using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Exam group title in the task list; the group in progress stands out.</summary>
    public sealed class GroupHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private UnityEngine.UI.Image background;

        public void Bind(int number, string groupName, bool isCurrent, bool isDone)
        {
            string state = isCurrent ? "идёт" : isDone ? "завершена" : "впереди";
            label.text = $"Группа {number}: {groupName}  <size=75%>· {state}</size>";
            label.color = isCurrent ? UiPalette.Accent : UiPalette.Muted;
            label.fontStyle = isCurrent ? FontStyles.Bold : FontStyles.Normal;
            background.color = isCurrent ? UiPalette.ActiveCard : Color.clear;
        }
    }
}
