using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>
    /// Main overlay: scenario title, mode switch, current group, progress, active step cards
    /// and short messages about violations and completed steps.
    /// </summary>
    public sealed class ScenarioHud : MonoBehaviour
    {
        [SerializeField] private ScenarioRunner runner;

        [Header("Header")]
        [SerializeField] private TMP_Text scenarioTitle;
        [SerializeField] private UnityEngine.UI.Button modeButtonTemplate;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button journalButton;

        [Header("Tasks")]
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text groupText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private StepCardView stepCardTemplate;
        [SerializeField] private UnityEngine.UI.Button showResultsButton;

        [Header("Other views")]
        [SerializeField] private MessageToast toast;
        [SerializeField] private JournalPanel journalPanel;
        [SerializeField] private ResultsPanel resultsPanel;

        private TemplateList<UnityEngine.UI.Button> modeButtons;
        private TemplateList<StepCardView> stepCards;
        private ScenarioSession session;

        private void Awake()
        {
            modeButtons = new TemplateList<UnityEngine.UI.Button>(modeButtonTemplate);
            stepCards = new TemplateList<StepCardView>(stepCardTemplate);
            restartButton.onClick.AddListener(runner.RestartRun);
            journalButton.onClick.AddListener(journalPanel.Show);
            showResultsButton.onClick.AddListener(resultsPanel.ShowLastReport);
            runner.SessionStarted += OnSessionStarted;
        }

        private void Start()
        {
            scenarioTitle.text = runner.Scenario != null ? runner.Scenario.Title : "Сценарий не задан";
        }

        private void OnDestroy()
        {
            runner.SessionStarted -= OnSessionStarted;
            Unsubscribe();
        }

        private void OnSessionStarted(ScenarioSession newSession)
        {
            Unsubscribe();
            session = newSession;
            session.StepChanged += OnStepChanged;
            session.ViolationRaised += OnViolationRaised;
            session.Finished += Refresh;
            RebuildModeButtons();
            Refresh();
        }

        private void Unsubscribe()
        {
            if (session == null)
            {
                return;
            }

            session.StepChanged -= OnStepChanged;
            session.ViolationRaised -= OnViolationRaised;
            session.Finished -= Refresh;
        }

        private void OnStepChanged(StepState step)
        {
            if (step.Status == StepStatus.Completed)
            {
                toast.Show($"Выполнено: {step.Definition.Instruction}", ToastKind.Success);
            }

            Refresh();
        }

        private void OnViolationRaised(StepState step, string message)
        {
            string consequence = step.Status switch
            {
                StepStatus.Failed => "Шаг провален.",
                StepStatus.Pending => "Дождитесь очереди этого шага.",
                _ => "Повторите шаг.",
            };
            toast.Show($"Нарушение: {message}\n{consequence}", ToastKind.Error);
        }

        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            int finished = session.Steps.Count(step => step.IsFinished);
            modeText.text = session.Mode.DisplayName;
            groupText.text = session.GroupCount > 1
                ? $"Группа {session.CurrentGroupNumber} из {session.GroupCount}: {session.CurrentGroupName}"
                : "Одна общая группа";
            progressText.text = $"Завершено шагов: {finished} из {session.Steps.Count}";

            stepCards.Clear();
            foreach (StepState step in session.ActiveSteps)
            {
                stepCards.Add().Bind(step);
            }

            showResultsButton.gameObject.SetActive(session.IsFinished);
        }

        private void RebuildModeButtons()
        {
            modeButtons.Clear();
            foreach (ScenarioMode mode in runner.Scenario.Modes.Where(mode => mode != null))
            {
                UnityEngine.UI.Button button = modeButtons.Add();
                button.GetComponentInChildren<TMP_Text>().text = mode.DisplayName;
                button.interactable = mode != session.Mode;
                button.onClick.AddListener(() => runner.StartRun(mode));
            }
        }
    }
}
