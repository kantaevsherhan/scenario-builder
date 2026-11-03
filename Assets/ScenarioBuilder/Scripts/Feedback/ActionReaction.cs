using System.Collections.Generic;
using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioBuilder.Feedback
{
    /// <summary>
    /// Plays scene effects when actions happen: particles, lights, sounds, animations.
    /// Everything is wired in the inspector through UnityEvents.
    /// </summary>
    public sealed class ActionReaction : ScenarioRunnerLink
    {
        [SerializeField, Tooltip("Действия, на которые реагирует объект.")]
        private List<ActionDefinition> actions = new();

        [SerializeField, Tooltip("Не реагировать, если действие оказалось нарушением.")]
        private bool skipOnViolation;

        [SerializeField, Tooltip("Что сделать, когда произошло одно из действий.")]
        private UnityEvent performed = new();

        [SerializeField, Tooltip("Что сделать в начале каждого прохождения, например выключить эффект.")]
        private UnityEvent runStarted = new();

        public UnityEvent Performed => performed;
        public UnityEvent RunStarted => runStarted;

        private void OnEnable()
        {
            if (Runner != null)
            {
                Runner.ActionRaised += OnActionRaised;
                Runner.SessionStarted += OnSessionStarted;
            }
        }

        private void OnDisable()
        {
            if (Runner != null)
            {
                Runner.ActionRaised -= OnActionRaised;
                Runner.SessionStarted -= OnSessionStarted;
            }
        }

        private void OnActionRaised(ActionDefinition action, bool causedViolation)
        {
            if (actions.Contains(action) && !(causedViolation && skipOnViolation))
            {
                performed.Invoke();
            }
        }

        private void OnSessionStarted(ScenarioSession session) => runStarted.Invoke();
    }
}
