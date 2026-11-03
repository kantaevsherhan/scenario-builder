using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>
    /// Raises an action from any UnityEvent: add this component and point the event
    /// (Slider, Toggle, your own component...) at <see cref="RaiseAction"/>.
    /// </summary>
    public sealed class UnityEventActionSource : ScenarioActionSource
    {
        [SerializeField, Tooltip("Действие, которое отправляется при вызове RaiseAction из UnityEvent.")]
        private ActionDefinition action;

        public void RaiseAction() => Raise(action);
    }
}
