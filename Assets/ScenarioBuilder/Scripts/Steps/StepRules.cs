using System;
using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Steps
{
    /// <summary>A scene state that must hold at the moment an action is performed.</summary>
    [Serializable]
    public sealed class StateRequirement
    {
        [SerializeField, Tooltip("Состояние сцены, которое проверяется в момент действия.")]
        private StateFlag flag;

        [SerializeField, Tooltip("Включено: флаг должен быть установлен. Выключено: снят.")]
        private bool mustBeSet = true;

        [SerializeField, Tooltip("Текст нарушения. Если пусто, он составится автоматически.")]
        private string violationMessage;

        public StateRequirement(StateFlag flag, bool mustBeSet, string violationMessage = "")
        {
            this.flag = flag;
            this.mustBeSet = mustBeSet;
            this.violationMessage = violationMessage;
        }

        public bool IsMet(ScenarioWorldState state) => flag == null || state.IsSet(flag) == mustBeSet;

        public string ViolationMessage
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(violationMessage))
                {
                    return violationMessage;
                }

                string expected = mustBeSet ? "включено" : "выключено";
                return $"Условие не выполнено: «{flag.DisplayName}» должно быть {expected}";
            }
        }
    }

    /// <summary>An action the step expects, optionally guarded by scene state requirements.</summary>
    [Serializable]
    public sealed class RequiredAction
    {
        [SerializeField, Tooltip("Действие, которого ждёт шаг.")]
        private ActionDefinition action;

        [SerializeField, Tooltip("Состояния сцены, которые должны выполняться в момент действия.")]
        private List<StateRequirement> requirements = new();

        public RequiredAction(ActionDefinition action, params StateRequirement[] requirements)
        {
            this.action = action;
            this.requirements = new List<StateRequirement>(requirements);
        }

        public ActionDefinition Action => action;

        public bool TryFindUnmetRequirement(ScenarioWorldState state, out StateRequirement unmet)
        {
            unmet = requirements.Find(requirement => requirement != null && !requirement.IsMet(state));
            return unmet != null;
        }
    }

    /// <summary>An action that counts as a mistake while the step is active.</summary>
    [Serializable]
    public sealed class ForbiddenAction
    {
        [SerializeField, Tooltip("Действие, которое во время шага считается ошибкой.")]
        private ActionDefinition action;

        [SerializeField, Tooltip("Текст нарушения. Если пусто, он составится автоматически.")]
        private string message;

        public ForbiddenAction(ActionDefinition action, string message = "")
        {
            this.action = action;
            this.message = message;
        }

        public ActionDefinition Action => action;

        public string Message => string.IsNullOrWhiteSpace(message)
            ? $"Недопустимое действие: «{action.DisplayName}»"
            : message;
    }
}
