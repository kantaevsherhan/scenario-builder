using ScenarioBuilder.Core;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Card of one active step in the task list.</summary>
    public sealed class StepCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text details;
        [SerializeField] private UnityEngine.UI.Image accent;

        public void Bind(StepState step)
        {
            title.text = step.Definition.Instruction;
            details.text = step.Attempt > 1
                ? $"Попытка {step.Attempt} · нарушений: {step.Violations}"
                : step.Violations > 0 ? $"Нарушений: {step.Violations}" : "Ожидает выполнения";
            accent.color = step.Violations > 0 ? UiPalette.Warning : UiPalette.Accent;
        }
    }
}
