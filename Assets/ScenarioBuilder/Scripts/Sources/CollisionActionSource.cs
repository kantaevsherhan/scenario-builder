using System.Collections.Generic;
using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    [RequireComponent(typeof(Collider))]
    public sealed class CollisionActionSource : ScenarioActionSource
    {
        [SerializeField, Tooltip("Тег объекта, столкновение с которым засчитывается. Если пусто, подходит любой объект.")]
        private string requiredTag = "Player";

        [SerializeField, Tooltip("Действие при столкновении.")]
        private ActionDefinition onCollision;

        public override IEnumerable<ActionDefinition> Actions => new[] { onCollision };

        protected override string DefaultHint => "Подойдите и коснитесь выделенного объекта (WASD или стрелки).";

        private void OnCollisionEnter(Collision collision)
        {
            if (string.IsNullOrEmpty(requiredTag) || collision.gameObject.CompareTag(requiredTag))
            {
                Raise(onCollision);
            }
        }
    }
}
