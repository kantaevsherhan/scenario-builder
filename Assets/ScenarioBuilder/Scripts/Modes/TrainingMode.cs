using System.Collections.Generic;
using ScenarioBuilder.Activation;
using UnityEngine;

namespace ScenarioBuilder.Modes
{
    /// <summary>All steps form one group; a violation never ends the step, the user simply tries again.</summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Modes/Training", fileName = "TrainingMode", order = 40)]
    public sealed class TrainingMode : ScenarioMode
    {
        [SerializeField, Tooltip("Шаги в порядке активации. Перетаскивайте, чтобы поменять порядок.")]
        private List<ActivationEntry> steps = new();

        public override IReadOnlyList<StepGroup> Groups => new[] { new StepGroup(DisplayName, steps) };

        public override ViolationResponse ResolveViolation(int violationsInGroup) => ViolationResponse.RetryStep;

        internal void Setup(IEnumerable<ActivationEntry> activationOrder)
        {
            steps = new List<ActivationEntry>(activationOrder);
        }
    }
}
