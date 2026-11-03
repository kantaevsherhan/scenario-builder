using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder.Activation
{
    /// <summary>A named part of a run with its own activation order (an exam section, for example).</summary>
    [Serializable]
    public sealed class StepGroup
    {
        [SerializeField, Tooltip("Название группы. Видно в списке шагов и в журнале.")]
        private string name;

        [SerializeField, Tooltip("Шаги группы в порядке активации. Перетаскивайте, чтобы поменять порядок.")]
        private List<ActivationEntry> steps = new();

        public StepGroup(string name, IEnumerable<ActivationEntry> steps)
        {
            this.name = name;
            this.steps = new List<ActivationEntry>(steps);
        }

        public string Name => name;
        public IReadOnlyList<ActivationEntry> Steps => steps;
    }
}
