using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>
    /// Holds a scene state flag ("valve is open"). Call SetOn / SetOff / Toggle from UnityEvents
    /// or code; steps can require the flag at the moment of an action.
    /// </summary>
    public sealed class StateFlagSource : ScenarioRunnerLink
    {
        [SerializeField] private StateFlag flag;

        [SerializeField, Tooltip("Значение флага при старте сцены.")]
        private bool initialValue;

        [SerializeField, Tooltip("Возвращать начальное значение при каждом новом прохождении.")]
        private bool resetOnRunStart = true;

        public bool IsOn => Runner != null && Runner.WorldState.IsSet(flag);

        private void Awake() => Set(initialValue);

        private void OnEnable()
        {
            if (Runner != null)
            {
                Runner.SessionStarted += OnSessionStarted;
            }
        }

        private void OnDisable()
        {
            if (Runner != null)
            {
                Runner.SessionStarted -= OnSessionStarted;
            }
        }

        public void Set(bool value)
        {
            if (Runner != null)
            {
                Runner.SetState(flag, value);
            }
        }

        public void SetOn() => Set(true);

        public void SetOff() => Set(false);

        public void Toggle() => Set(!IsOn);

        private void OnSessionStarted(ScenarioSession session)
        {
            if (resetOnRunStart)
            {
                Set(initialValue);
            }
        }
    }
}
