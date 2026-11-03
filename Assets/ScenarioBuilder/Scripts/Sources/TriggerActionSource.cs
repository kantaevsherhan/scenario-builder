using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    [RequireComponent(typeof(Collider))]
    public sealed class TriggerActionSource : ScenarioActionSource
    {
        [SerializeField, Tooltip("Тег объекта, на который реагирует триггер. Пусто — любой объект.")]
        private string requiredTag = "Player";

        [SerializeField, Tooltip("Действие при входе в зону. Можно оставить пустым.")]
        private ActionDefinition onEnter;

        [SerializeField, Tooltip("Действие при выходе из зоны. Можно оставить пустым.")]
        private ActionDefinition onExit;

        public override IEnumerable<ActionDefinition> Actions => new[] { onEnter, onExit };

        protected override string DefaultHint => "Дойдите до выделенного места (WASD или стрелки).";

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
