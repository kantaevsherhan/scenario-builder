using System.Linq;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>What the spotlight points at: a UI element, a 3D object projected to the screen, or nothing.</summary>
    public readonly struct SpotlightTarget
    {
        private readonly GameObject target;

        public SpotlightTarget(GameObject target)
        {
            this.target = target;
        }

        public bool TryGetScreenRect(Camera uiCamera, out Rect rect)
        {
            rect = default;
            if (target == null || !target.activeInHierarchy)
            {
                return false;
            }

            return target.transform is RectTransform rectTransform && target.GetComponentInParent<Canvas>() != null
                ? TryGetUiRect(rectTransform, uiCamera, out rect)
                : TryGetWorldRect(out rect);
        }

        private static bool TryGetUiRect(RectTransform rectTransform, Camera uiCamera, out Rect rect)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private bool TryGetWorldRect(out Rect rect)
        {
            rect = default;
            Camera camera = Camera.main;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer is not ParticleSystemRenderer)
                .ToArray();
            if (camera == null || renderers.Length == 0)
            {
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
                Vector3 point = camera.WorldToScreenPoint(bounds.min + Vector3.Scale(bounds.size, corner));
                if (point.z <= 0f)
                {
                    return false;
                }

                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }
    }
}
