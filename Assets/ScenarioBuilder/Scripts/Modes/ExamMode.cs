using System.Collections.Generic;
using ScenarioBuilder.Activation;
using UnityEngine;

namespace ScenarioBuilder.Modes
{
    /// <summary>
    /// Steps are split into groups. A violated step fails without a retry;
    /// once a group reaches the violation threshold, its unfinished steps are skipped.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Modes/Exam", fileName = "ExamMode", order = 41)]
    public sealed class ExamMode : ScenarioMode
    {
        [SerializeField, Min(1), Tooltip("Сколько нарушений в группе допускается, прежде чем её оставшиеся шаги будут пропущены.")]
        private int violationThreshold = 2;

        [SerializeField, Tooltip("Группы проходят по очереди. Внутри группы — свой порядок активации.")]
        private List<StepGroup> groups = new();

        public int ViolationThreshold => violationThreshold;

        public override IReadOnlyList<StepGroup> Groups => groups;

        public override ViolationResponse ResolveViolation(int violationsInGroup) =>
            violationsInGroup >= violationThreshold
                ? ViolationResponse.FailStepAndSkipGroup
                : ViolationResponse.FailStep;

        internal void Setup(int threshold, IEnumerable<StepGroup> stepGroups)
        {
            violationThreshold = Mathf.Max(1, threshold);
            groups = new List<StepGroup>(stepGroups);
        }
    }
}
