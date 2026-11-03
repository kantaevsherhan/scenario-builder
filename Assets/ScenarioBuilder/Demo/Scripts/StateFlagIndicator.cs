using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.Demo
{
    /// <summary>Shows a state flag on a 3D object color and a text label.</summary>
    public sealed class StateFlagIndicator : ScenarioRunnerLink
    {
        [SerializeField] private StateFlag flag;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string onText = "Вкл.";
        [SerializeField] private string offText = "Выкл.";
        [SerializeField] private Color onColor = new(0.24f, 0.86f, 0.59f);
        [SerializeField] private Color offColor = new(1f, 0.36f, 0.36f);

        private MaterialPropertyBlock properties;

        private void OnEnable()
        {
            if (Runner != null)
            {
                Runner.WorldState.Changed += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (Runner != null)
            {
                Runner.WorldState.Changed -= OnStateChanged;
            }
        }

        private void Start() => Show(Runner != null && Runner.WorldState.IsSet(flag));

        private void OnStateChanged(StateFlag changed, bool value)
        {
            if (changed == flag)
            {
                Show(value);
            }
        }

        private void Show(bool isOn)
        {
            Color color = isOn ? onColor : offColor;
            if (targetRenderer != null)
            {
                properties ??= new MaterialPropertyBlock();
                properties.SetColor("_Color", color);
                targetRenderer.SetPropertyBlock(properties);
            }

            if (label != null)
            {
                label.text = isOn ? onText : offText;
                label.color = color;
            }
        }
    }
}
