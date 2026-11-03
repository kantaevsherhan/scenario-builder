using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Steps
{
    /// <summary>
    /// Step completed by one or more actions performed in order.
    /// One action — a simple step ("press the button"), several — a composite one
    /// ("enter zone A, then zone B"). Each action may require a scene state.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Action Step", fileName = "Step", order = 0)]
    public sealed class ActionStep : StepDefinition
    {
        [SerializeField, Tooltip("Действия, которые нужно выполнить по порядку. Шаг завершается после последнего.")]
        private List<RequiredAction> requiredActions = new();

        [SerializeField, Tooltip("Действие из списка, выполненное раньше своей очереди, считается нарушением. " +
                                 "Если выключено, такое действие просто игнорируется.")]
        private bool wrongOrderIsViolation = true;

        [SerializeField, Tooltip("Действия, которые во время этого шага считаются ошибкой.")]
        private List<ForbiddenAction> forbiddenActions = new();

        public IReadOnlyList<RequiredAction> RequiredActions => requiredActions;

        public override bool Involves(ActionDefinition action) =>
            action != null && IndexOfRequired(action, 0) >= 0;

        public override IEnumerable<ActionDefinition> ExpectedActions(StepContext context)
        {
            if (context.Progress < requiredActions.Count && requiredActions[context.Progress].Action != null)
            {
                yield return requiredActions[context.Progress].Action;
            }
        }

        public override StepEvaluation Evaluate(ActionDefinition action, StepContext context)
        {
            ForbiddenAction forbidden = forbiddenActions.Find(item => item.Action != null && item.Action == action);
            if (forbidden != null)
            {
                return StepEvaluation.Violation(forbidden.Message);
            }

            if (context.Progress >= requiredActions.Count)
            {
                return StepEvaluation.Ignored;
            }

            RequiredAction expected = requiredActions[context.Progress];
            if (expected.Action != action)
            {
                bool isAheadOfOrder = IndexOfRequired(action, context.Progress + 1) >= 0;
                return isAheadOfOrder && wrongOrderIsViolation
                    ? StepEvaluation.Violation($"Нарушен порядок: сначала «{expected.Action.DisplayName}»")
                    : StepEvaluation.Ignored;
            }

            if (expected.TryFindUnmetRequirement(context.WorldState, out StateRequirement unmet))
            {
                return StepEvaluation.Violation(unmet.ViolationMessage);
            }

            context.Progress++;
            return context.Progress == requiredActions.Count ? StepEvaluation.Completed : StepEvaluation.Progressed;
        }

        public override IEnumerable<string> Validate()
        {
            if (requiredActions.Count == 0)
            {
                yield return "не задано ни одного действия для выполнения.";
            }

            if (requiredActions.Exists(item => item.Action == null))
            {
                yield return "в списке действий есть пустые ячейки.";
            }

            if (forbiddenActions.Exists(item => item.Action == null))
            {
                yield return "в списке запрещённых действий есть пустые ячейки.";
            }
        }

        internal void Setup(string stepId, string stepInstruction, IEnumerable<RequiredAction> required,
            IEnumerable<ForbiddenAction> forbidden = null, bool isWrongOrderViolation = true)
        {
            SetIdentity(stepId, stepInstruction);
            requiredActions = new List<RequiredAction>(required);
            forbiddenActions = forbidden == null ? new List<ForbiddenAction>() : new List<ForbiddenAction>(forbidden);
            wrongOrderIsViolation = isWrongOrderViolation;
        }

        private int IndexOfRequired(ActionDefinition action, int startIndex)
        {
            for (int i = startIndex; i < requiredActions.Count; i++)
            {
                if (requiredActions[i].Action != null && requiredActions[i].Action == action)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
