using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>Raises an action when an object with the given tag collides with this object.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CollisionActionSource : ScenarioActionSource
    {
        [SerializeField, Tooltip("Тег объекта, столкновение с которым засчитывается. Пусто — любой объект.")]
        private string requiredTag = "Player";

        [SerializeField, Tooltip("Действие при столкновении.")]
        private ActionDefinition onCollision;

        private void OnCollisionEnter(Collision collision)
        {
            if (string.IsNullOrEmpty(requiredTag) || collision.gameObject.CompareTag(requiredTag))
            {
                Raise(onCollision);
            }
        }
    }
}
