using ScenarioBuilder.Core;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    public sealed class StepCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text details;
        [SerializeField] private UnityEngine.UI.Image accent;
        [SerializeField] private UnityEngine.UI.Image background;

        public void Bind(StepState step)
        {
            Color statusColor = UiPalette.For(step.Status);
            bool isActive = step.Status == StepStatus.Active;
            bool isPending = step.Status == StepStatus.Pending;

            title.text = step.Definition.Instruction;
            title.color = isPending ? UiPalette.Muted : UiPalette.Text;
            title.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            accent.color = isPending ? UiPalette.Card : statusColor;
            background.color = isActive ? UiPalette.ActiveCard : UiPalette.Card;

            details.gameObject.SetActive(!isPending);
            details.color = statusColor;
            details.text = UiPalette.Label(step.Status).ToUpperInvariant() + Extra(step);
        }

        private static string Extra(StepState step) => step.Status switch
        {
            StepStatus.Active when step.Violations > 0 => $"  ·  попытка {step.Attempt}, нарушений: {step.Violations}",
            StepStatus.Completed when step.Attempt > 1 => $"  ·  с {step.Attempt}-й попытки",
            _ => string.Empty,
        };
    }
}
