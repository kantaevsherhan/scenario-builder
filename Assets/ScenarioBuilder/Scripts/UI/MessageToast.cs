using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    public enum ToastKind
    {
        Info,
        Success,
        Error,
    }

    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MessageToast : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField, Min(0.5f)] private float visibleSeconds = 3f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.4f;

        private CanvasGroup canvasGroup;
        private float hideAt;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            float remaining = hideAt - Time.unscaledTime;
            canvasGroup.alpha = Mathf.Clamp01(remaining / fadeSeconds);
        }

        public void Show(string message, ToastKind kind)
        {
            text.text = message;
            Color color = kind switch
            {
                ToastKind.Success => UiPalette.Success,
                ToastKind.Error => UiPalette.Error,
                _ => UiPalette.Accent,
            };
            background.color = new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 0.95f);
            text.color = Color.Lerp(color, Color.white, 0.6f);
            hideAt = Time.unscaledTime + visibleSeconds + fadeSeconds;
        }
    }
}
