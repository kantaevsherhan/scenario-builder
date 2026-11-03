using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioBuilder.Demo
{
    /// <summary>
    /// Demo pressure slider: fires UnityEvents when the value reaches the maximum or drops to the minimum.
    /// The events are wired to <see cref="UnityEventActionSource"/> components in the scene.
    /// </summary>
    public sealed class PressureControl : ScenarioRunnerLink
    {
        [SerializeField] private UnityEngine.UI.Slider slider;
        [SerializeField] private TMP_Text valueLabel;

        [SerializeField, Tooltip("Вызывается, когда давление достигает максимума.")]
        private UnityEvent reachedMax = new();

        [SerializeField, Tooltip("Вызывается, когда давление опускается до минимума.")]
        private UnityEvent reachedMin = new();

        public UnityEvent ReachedMax => reachedMax;
        public UnityEvent ReachedMin => reachedMin;

        private bool isAtMax;
        private bool isAtMin = true;

        private void Awake() => slider.onValueChanged.AddListener(OnValueChanged);

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

        private void Start() => UpdateLabel(slider.value);

        private void OnValueChanged(float value)
        {
            UpdateLabel(value);

            bool atMax = value >= slider.maxValue;
            bool atMin = value <= slider.minValue;
            if (atMax && !isAtMax)
            {
                reachedMax.Invoke();
            }

            if (atMin && !isAtMin)
            {
                reachedMin.Invoke();
            }

            isAtMax = atMax;
            isAtMin = atMin;
        }

        private void UpdateLabel(float value)
        {
            float percent = Mathf.InverseLerp(slider.minValue, slider.maxValue, value) * 100f;
            valueLabel.text = $"{percent:0}%";
        }

        private void OnSessionStarted(ScenarioSession session)
        {
            slider.SetValueWithoutNotify(slider.minValue);
            isAtMax = false;
            isAtMin = true;
            UpdateLabel(slider.minValue);
        }
    }
}
