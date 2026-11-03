using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Colors and Russian labels for step statuses, shared by all views.</summary>
    public static class UiPalette
    {
        public static readonly Color Accent = new(0.31f, 0.66f, 1f);
        public static readonly Color Success = new(0.24f, 0.86f, 0.59f);
        public static readonly Color Error = new(1f, 0.36f, 0.36f);
        public static readonly Color Warning = new(0.96f, 0.72f, 0.25f);
        public static readonly Color Muted = new(0.55f, 0.6f, 0.7f);

        public static Color For(StepStatus status) => status switch
        {
            StepStatus.Completed => Success,
            StepStatus.Failed => Error,
            StepStatus.Skipped => Warning,
            StepStatus.Active => Accent,
            _ => Muted,
        };

        public static Color For(StepResult result) => result switch
        {
            StepResult.Completed => Success,
            StepResult.Failed => Error,
            StepResult.Skipped => Warning,
            _ => Muted,
        };

        public static string Label(StepResult result) => result switch
        {
            StepResult.Completed => "Выполнен",
            StepResult.Failed => "Провален",
            StepResult.Skipped => "Пропущен",
            _ => "Не завершён",
        };
    }
}
