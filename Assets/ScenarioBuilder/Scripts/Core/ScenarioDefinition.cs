using System.Collections.Generic;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEngine;

namespace ScenarioBuilder.Core
{
    /// <summary>
    /// A scenario: the library of reusable steps and the modes that run them.
    /// Steps are listed once here and only referenced by the modes.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Scenario", fileName = "Scenario", order = 1)]
    public sealed class ScenarioDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Название сценария для интерфейса и журнала.")]
        private string title;

        [SerializeField, Tooltip("Все шаги сценария. Режимы ссылаются на эти шаги, а не копируют их.")]
        private List<StepDefinition> steps = new();

        [SerializeField, Tooltip("Режимы прохождения. Первый запускается по умолчанию.")]
        private List<ScenarioMode> modes = new();

        public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
        public IReadOnlyList<StepDefinition> Steps => steps;
        public IReadOnlyList<ScenarioMode> Modes => modes;

        internal void Setup(string scenarioTitle, IEnumerable<StepDefinition> stepLibrary, IEnumerable<ScenarioMode> scenarioModes)
        {
            title = scenarioTitle;
            steps = new List<StepDefinition>(stepLibrary);
            modes = new List<ScenarioMode>(scenarioModes);
        }
    }
}
