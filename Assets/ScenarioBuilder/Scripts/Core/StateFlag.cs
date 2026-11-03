using UnityEngine;

namespace ScenarioBuilder.Core
{
    /// <summary>
    /// A named on/off state of the scene ("valve is open").
    /// Steps can require a flag to be on or off at the moment of an action.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/State Flag", fileName = "StateFlag", order = 21)]
    public sealed class StateFlag : ScriptableObject
    {
        [SerializeField, Tooltip("Понятное название состояния. Показывается в сообщениях о нарушениях.")]
        private string displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
