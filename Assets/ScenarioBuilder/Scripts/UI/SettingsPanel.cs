using UnityEngine;

namespace ScenarioBuilder.UI
{
    public sealed class SettingsPanel : MonoBehaviour, IClosablePanel
    {
        [SerializeField] private GameObject root;
        [SerializeField] private UnityEngine.UI.Toggle tourToggle;
        [SerializeField] private UnityEngine.UI.Toggle highlightToggle;
        [SerializeField] private UnityEngine.UI.Button replayTourButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private GuidedTour tour;

        public bool IsOpen => root.activeSelf;

        private void Awake()
        {
            tourToggle.onValueChanged.AddListener(value => ScenarioPreferences.GuidedTour = value);
            highlightToggle.onValueChanged.AddListener(value => ScenarioPreferences.HighlightTargets = value);
            replayTourButton.onClick.AddListener(ReplayTour);
            closeButton.onClick.AddListener(Hide);
            root.SetActive(false);
        }

        public void Show()
        {
            if (tour.IsOpen)
            {
                tour.Hide();
            }

            tourToggle.SetIsOnWithoutNotify(ScenarioPreferences.GuidedTour);
            highlightToggle.SetIsOnWithoutNotify(ScenarioPreferences.HighlightTargets);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        public void Hide() => root.SetActive(false);

        private void ReplayTour()
        {
            Hide();
            ScenarioPreferences.GuidedTour = true;
            tour.StartIntro();
        }
    }
}
