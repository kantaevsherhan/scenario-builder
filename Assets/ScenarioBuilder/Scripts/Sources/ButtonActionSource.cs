using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>Raises an action when the UI button on the same object is clicked.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ButtonActionSource : ScenarioActionSource
    {
        [SerializeField, Tooltip("Действие при нажатии кнопки.")]
        private ActionDefinition action;

        public override IEnumerable<ActionDefinition> Actions => new[] { action };

        private UnityEngine.UI.Button button;

        private void Awake() => button = GetComponent<UnityEngine.UI.Button>();

        private void OnEnable() => button.onClick.AddListener(OnClick);

        private void OnDisable() => button.onClick.RemoveListener(OnClick);

        private void OnClick() => Raise(action);
    }
}
