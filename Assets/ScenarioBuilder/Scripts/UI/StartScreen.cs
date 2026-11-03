using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>
    /// Screen shown before the first run: scenario title, mode choice and the Play button.
    /// The run (and the journal) starts only when Play is pressed.
    /// </summary>
    public sealed class StartScreen : MonoBehaviour
    {
        [SerializeField] private ScenarioRunner runner;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text modeDescription;
        [SerializeField] private UnityEngine.UI.Button modeButtonTemplate;
        [SerializeField] private UnityEngine.UI.Button playButton;

        private TemplateList<UnityEngine.UI.Button> modeButtons;
        private readonly List<ScenarioMode> modes = new();
        private ScenarioMode selected;

        public bool IsOpen => root.activeSelf;

        private void Awake()
        {
            modeButtons = new TemplateList<UnityEngine.UI.Button>(modeButtonTemplate);
            playButton.onClick.AddListener(Play);
            runner.SessionStarted += OnSessionStarted;
        }

        private void OnDestroy() => runner.SessionStarted -= OnSessionStarted;

        private void OnSessionStarted(ScenarioSession session) => root.SetActive(false);

        private void Start()
        {
            if (runner.Session == null)
            {
                Show();
            }
            else
            {
                root.SetActive(false);
            }
        }

        public void Show()
        {
            ScenarioDefinition scenario = runner.Scenario;
            title.text = scenario != null ? scenario.Title : "Сценарий не задан";
            modes.Clear();
            if (scenario != null)
            {
                modes.AddRange(scenario.Modes.Where(mode => mode != null));
            }

            modeButtons.Clear();
            foreach (ScenarioMode mode in modes)
            {
                UnityEngine.UI.Button button = modeButtons.Add();
                button.GetComponentInChildren<TMP_Text>().text = mode.DisplayName;
                button.onClick.AddListener(() => Select(mode));
            }

            Select(runner.Session != null ? runner.Session.Mode : runner.DefaultMode);
            playButton.interactable = selected != null;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        private void Select(ScenarioMode mode)
        {
            selected = mode;
            for (int i = 0; i < modes.Count; i++)
            {
                // A disabled button is drawn as the selected one (see the demo UI builder).
                modeButtons[i].interactable = modes[i] != mode;
            }

            modeDescription.text = mode != null ? mode.Description : string.Empty;
        }

        private void Play()
        {
            if (selected == null)
            {
                return;
            }

            runner.StartRun(selected);
        }
    }
}
