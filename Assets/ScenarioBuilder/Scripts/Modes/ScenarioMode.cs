using System.Collections.Generic;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using UnityEngine;

namespace ScenarioBuilder.Modes
{
    public enum OutOfTurnPolicy
    {
        [InspectorName("Считать нарушением")]
        Violation,

        [InspectorName("Игнорировать (шаг неактивен до своей очереди)")]
        Ignore,
    }

    /// <summary>What happens to a step after a violation.</summary>
    public enum ViolationResponse
    {
        /// <summary>The step stays in place, the user tries again.</summary>
        RetryStep,

        /// <summary>The step is failed, the run goes on.</summary>
        FailStep,

        /// <summary>The step is failed and the rest of its group is skipped.</summary>
        FailStepAndSkipGroup,
    }

    /// <summary>
    /// A way of running the same steps: their grouping, activation order and the reaction to violations.
    /// Derive from this class to add a new mode.
    /// </summary>
    public abstract class ScenarioMode : ScriptableObject
    {
        [SerializeField, Tooltip("Название режима для интерфейса и журнала.")]
        private string displayName;

        [SerializeField, Tooltip("Что делать с действием, относящимся к шагу, очередь которого ещё не наступила.")]
        private OutOfTurnPolicy outOfTurnPolicy = OutOfTurnPolicy.Violation;

        [SerializeField, Tooltip("Анализатор, которым оценивается прохождение в этом режиме.")]
        private ScenarioAnalyzer defaultAnalyzer;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public OutOfTurnPolicy OutOfTurnPolicy => outOfTurnPolicy;
        public ScenarioAnalyzer DefaultAnalyzer => defaultAnalyzer;

        /// <summary>Groups run one after another, each with its own activation order.</summary>
        public abstract IReadOnlyList<StepGroup> Groups { get; }

        /// <param name="violationsInGroup">Violations in the current group including this one.</param>
        public abstract ViolationResponse ResolveViolation(int violationsInGroup);

        internal void SetCommon(string modeName, OutOfTurnPolicy policy, ScenarioAnalyzer analyzer)
        {
            displayName = modeName;
            outOfTurnPolicy = policy;
            defaultAnalyzer = analyzer;
        }
    }
}
