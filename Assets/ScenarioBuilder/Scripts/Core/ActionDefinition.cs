using UnityEngine;

namespace ScenarioBuilder.Core
{
    /// <summary>
    /// A named user action ("press the power button", "enter zone A").
    /// Event sources raise it, steps react to it.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Action", fileName = "Action", order = 20)]
    public sealed class ActionDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Понятное название действия. Показывается в журнале и сообщениях.")]
        private string displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
