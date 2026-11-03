using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>
    /// Base for components that turn scene events into scenario actions.
    /// To add a new source, derive from this class and call <see cref="Raise"/>.
    /// </summary>
    public abstract class ScenarioActionSource : ScenarioRunnerLink
    {
        [SerializeField, TextArea(1, 2), Tooltip("Подсказка в обучающем туре: как выполнить действие. Пусто — стандартный текст.")]
        private string hint;

        /// <summary>Actions this source can send. Feedback and hints use it to find the object of a step.</summary>
        public abstract IEnumerable<ActionDefinition> Actions { get; }

        public string Hint => string.IsNullOrWhiteSpace(hint) ? DefaultHint : hint;

        protected virtual string DefaultHint => "Выполните действие с выделенным объектом.";

        protected void Raise(ActionDefinition action)
        {
            if (action == null)
            {
                return;
            }

            if (Runner == null)
            {
                Debug.LogWarning($"{name}: в сцене нет ScenarioRunner, действие «{action.DisplayName}» потеряно.", this);
                return;
            }

            Runner.RaiseAction(action);
        }
    }
}
