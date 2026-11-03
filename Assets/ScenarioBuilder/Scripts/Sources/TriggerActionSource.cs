using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>Raises actions when an object with the given tag enters or leaves this trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TriggerActionSource : ScenarioActionSource
    {
        [SerializeField, Tooltip("Тег объекта, на который реагирует триггер. Пусто — любой объект.")]
        private string requiredTag = "Player";

        [SerializeField, Tooltip("Действие при входе в зону. Можно оставить пустым.")]
        private ActionDefinition onEnter;

        [SerializeField, Tooltip("Действие при выходе из зоны. Можно оставить пустым.")]
        private ActionDefinition onExit;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (Matches(other))
            {
                Raise(onEnter);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (Matches(other))
            {
                Raise(onExit);
            }
        }

        private bool Matches(Component other) => string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);
    }
}
