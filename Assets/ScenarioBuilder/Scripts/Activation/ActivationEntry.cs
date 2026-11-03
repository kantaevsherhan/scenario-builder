using System;
using ScenarioBuilder.Steps;
using UnityEngine;

namespace ScenarioBuilder.Activation
{
    public enum ActivationMode
    {
        [InspectorName("После предыдущих")]
        AfterPrevious,

        [InspectorName("Вместе с предыдущим")]
        WithPrevious,
    }

    [Serializable]
    public sealed class ActivationEntry
    {
        [SerializeField] private StepDefinition step;

        [SerializeField, Tooltip("«После предыдущих» — шаг откроется, когда завершатся все шаги выше. " +
                                 "«Вместе с предыдущим» — шаг откроется одновременно с шагом выше.")]
        private ActivationMode activation;

        public ActivationEntry(StepDefinition step, ActivationMode activation = ActivationMode.AfterPrevious)
        {
            this.step = step;
            this.activation = activation;
        }

        public StepDefinition Step => step;
        public ActivationMode Activation => activation;
    }
}
