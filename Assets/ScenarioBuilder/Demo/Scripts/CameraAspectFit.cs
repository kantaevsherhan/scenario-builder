using UnityEngine;

namespace ScenarioBuilder.Demo
{
    /// <summary>
    /// Keeps the horizontal view of the reference aspect on narrower screens (4:3, 16:10, a resized browser window),
    /// so the side panels never cover the scene. Wider screens keep the authored vertical field of view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraAspectFit : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float referenceAspect = 16f / 9f;

        private Camera target;
        private float verticalFov;
        private float appliedAspect;

        private void Awake()
        {
            target = GetComponent<Camera>();
            verticalFov = target.fieldOfView;
        }

        private void LateUpdate()
        {
            float aspect = target.aspect;
            if (Mathf.Approximately(aspect, appliedAspect))
            {
                return;
            }

            appliedAspect = aspect;
            float halfWidth = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) * referenceAspect;
            float fitted = 2f * Mathf.Atan(halfWidth / aspect) * Mathf.Rad2Deg;
            target.fieldOfView = Mathf.Max(verticalFov, fitted);
        }
    }
}
