using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Steps
{
    /// <summary>
    /// A reusable scenario step. The same asset is referenced by every mode,
    /// modes only decide when the step becomes active and what a violation costs.
    /// Derive from this class to add a new kind of step.
    /// </summary>
    public abstract class StepDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Уникальный идентификатор шага. Используется в журнале. По умолчанию — имя ассета.")]
        private string id;

        [SerializeField, TextArea(1, 3), Tooltip("Текст задания, который видит пользователь.")]
        private string instruction;

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string Instruction => string.IsNullOrWhiteSpace(instruction) ? name : instruction;

        /// <summary>Reacts to an action while the step is active.</summary>
        public abstract StepEvaluation Evaluate(ActionDefinition action, StepContext context);

        /// <summary>True when the action belongs to this step. Used to detect out-of-turn actions.</summary>
        public abstract bool Involves(ActionDefinition action);

        /// <summary>Configuration problems shown in the inspector.</summary>
        public virtual IEnumerable<string> Validate()
        {
            yield break;
        }

        protected void SetIdentity(string stepId, string stepInstruction)
        {
            id = stepId;
            instruction = stepInstruction;
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name;
            }
        }
    }
}
