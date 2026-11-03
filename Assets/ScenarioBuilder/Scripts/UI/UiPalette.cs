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
        public static readonly Color Text = new(0.91f, 0.93f, 0.96f);
        public static readonly Color Card = new(0.133f, 0.165f, 0.227f);
        public static readonly Color ActiveCard = new(0.15f, 0.24f, 0.37f);

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

        public static string Label(StepStatus status) => status switch
        {
            StepStatus.Active => "Сейчас",
            StepStatus.Completed => "Выполнен",
            StepStatus.Failed => "Провален",
            StepStatus.Skipped => "Пропущен",
            _ => "Ожидает очереди",
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
