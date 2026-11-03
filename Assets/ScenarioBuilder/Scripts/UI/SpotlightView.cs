using System;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>
    /// Dims the screen except for a cut-out around the target and shows a callout next to it.
    /// The cut-out is made of four dim panels, so clicks inside it reach the highlighted element.
    /// </summary>
    public sealed class SpotlightView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;
        [SerializeField] private RectTransform frame;

        [Header("Callout")]
        [SerializeField] private RectTransform callout;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text counter;
        [SerializeField] private UnityEngine.UI.Button nextButton;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private UnityEngine.UI.Button skipButton;

        [SerializeField, Min(0f)] private float padding = 10f;
        [SerializeField, Min(0f)] private float gap = 18f;

        private Canvas canvas;
        private SpotlightTarget target;
        private Rect hole;
        private bool snapHole;

        public event Action NextClicked;
        public event Action SkipClicked;

        public bool IsVisible => root.gameObject.activeSelf;

        private void Awake()
        {
            canvas = root.GetComponentInParent<Canvas>().rootCanvas;
            nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            skipButton.onClick.AddListener(() => SkipClicked?.Invoke());
            root.gameObject.SetActive(false);
        }

        public void Show(SpotlightTarget newTarget, string titleText, string bodyText, string counterText, string nextText)
        {
            snapHole = !IsVisible;
            target = newTarget;
            title.text = titleText;
            body.text = bodyText;
            counter.text = counterText;
            nextLabel.text = nextText;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(callout);
            LateUpdate();
        }

        public void Hide() => root.gameObject.SetActive(false);

        private void LateUpdate()
        {
            if (!IsVisible)
            {
                return;
            }

            Rect bounds = root.rect;
            Rect wanted = TryGetTargetRect(out Rect targetRect) ? Expand(targetRect, padding) : new Rect(bounds.center, Vector2.zero);
            hole = snapHole ? wanted : Lerp(hole, wanted, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            snapHole = false;

            Place(top, Rect.MinMaxRect(bounds.xMin, hole.yMax, bounds.xMax, bounds.yMax));
            Place(bottom, Rect.MinMaxRect(bounds.xMin, bounds.yMin, bounds.xMax, hole.yMin));
            Place(left, Rect.MinMaxRect(bounds.xMin, hole.yMin, hole.xMin, hole.yMax));
            Place(right, Rect.MinMaxRect(hole.xMax, hole.yMin, bounds.xMax, hole.yMax));
            Place(frame, hole);
            frame.gameObject.SetActive(hole.width > 1f);
            PlaceCallout(bounds);
        }

        private bool TryGetTargetRect(out Rect rect)
        {
            rect = default;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return target.TryGetScreenRect(uiCamera, out Rect screenRect) && ToLocal(screenRect, uiCamera, out rect);
        }

        private bool ToLocal(Rect screenRect, Camera uiCamera, out Rect local)
        {
            bool okMin = RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenRect.min, uiCamera, out Vector2 min);
            bool okMax = RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenRect.max, uiCamera, out Vector2 max);
            local = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return okMin && okMax;
        }

        /// <summary>Right of the hole, then left, below, above; centered when there is no hole.</summary>
        private void PlaceCallout(Rect bounds)
        {
            Vector2 size = callout.rect.size;
            Vector2 half = size / 2f;
            Rect area = Expand(bounds, -gap);
            Vector2 center;

            if (hole.width < 1f)
            {
                center = bounds.center;
            }
            else if (hole.xMax + gap + size.x <= area.xMax)
            {
                center = new Vector2(hole.xMax + gap + half.x, hole.center.y);
            }
            else if (hole.xMin - gap - size.x >= area.xMin)
            {
                center = new Vector2(hole.xMin - gap - half.x, hole.center.y);
            }
            else if (hole.yMin - gap - size.y >= area.yMin)
            {
                center = new Vector2(hole.center.x, hole.yMin - gap - half.y);
            }
            else
            {
                center = new Vector2(hole.center.x, hole.yMax + gap + half.y);
            }

            center.x = Mathf.Clamp(center.x, area.xMin + half.x, area.xMax - half.x);
            center.y = Mathf.Clamp(center.y, area.yMin + half.y, area.yMax - half.y);
            callout.anchoredPosition = center;
        }

        private static void Place(RectTransform panel, Rect rect)
        {
            panel.anchoredPosition = rect.center;
            panel.sizeDelta = new Vector2(Mathf.Max(0f, rect.width), Mathf.Max(0f, rect.height));
        }

        private static Rect Expand(Rect rect, float amount) =>
            Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);

        private static Rect Lerp(Rect from, Rect to, float t) =>
            Rect.MinMaxRect(Mathf.Lerp(from.xMin, to.xMin, t), Mathf.Lerp(from.yMin, to.yMin, t),
                Mathf.Lerp(from.xMax, to.xMax, t), Mathf.Lerp(from.yMax, to.yMax, t));
    }
}
