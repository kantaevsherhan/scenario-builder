using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>
    /// Main overlay: scenario title, mode switch, the list of steps with their state
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
        [SerializeField] private UnityEngine.UI.Button quitButton;

        [Header("Tasks")]
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text groupText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private StepCardView stepCardTemplate;
        [SerializeField] private GroupHeaderView groupHeaderTemplate;
        [SerializeField] private UnityEngine.UI.ScrollRect stepList;
        [SerializeField] private UnityEngine.UI.Button showResultsButton;

        [Header("Other views")]
        [SerializeField] private MessageToast toast;
        [SerializeField] private JournalPanel journalPanel;
        [SerializeField] private ResultsPanel resultsPanel;

        private TemplateList<UnityEngine.UI.Button> modeButtons;
        private TemplateList<StepCardView> stepCards;
        private TemplateList<GroupHeaderView> groupHeaders;
        private readonly List<(StepState Step, StepCardView View)> cards = new();
        private readonly List<GroupHeaderView> headers = new();
        private ScenarioSession session;
        private StepCardView scrolledTo;

        private void Awake()
        {
            modeButtons = new TemplateList<UnityEngine.UI.Button>(modeButtonTemplate);
            stepCards = new TemplateList<StepCardView>(stepCardTemplate);
            groupHeaders = new TemplateList<GroupHeaderView>(groupHeaderTemplate);
            restartButton.onClick.AddListener(runner.RestartRun);
            journalButton.onClick.AddListener(journalPanel.Show);
            quitButton.onClick.AddListener(ApplicationExit.Quit);
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
            BuildStepList();
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

            foreach ((StepState step, StepCardView view) in cards)
            {
                view.Bind(step);
            }

            for (int i = 0; i < headers.Count; i++)
            {
                bool isCurrent = !session.IsFinished && i == session.CurrentGroupNumber - 1;
                bool isDone = session.IsFinished || i < session.CurrentGroupNumber - 1;
                headers[i].Bind(i + 1, session.Steps.First(step => step.GroupIndex == i).GroupName, isCurrent, isDone);
            }

            showResultsButton.gameObject.SetActive(session.IsFinished);
            ScrollToFirstActive();
        }

        /// <summary>Rows are created once per run and only re-bound afterwards.</summary>
        private void BuildStepList()
        {
            stepCards.Clear();
            groupHeaders.Clear();
            cards.Clear();
            headers.Clear();
            scrolledTo = null;

            int lastGroup = -1;
            foreach (StepState step in session.Steps)
            {
                if (session.GroupCount > 1 && step.GroupIndex != lastGroup)
                {
                    headers.Add(groupHeaders.Add());
                    lastGroup = step.GroupIndex;
                }

                cards.Add((step, stepCards.Add()));
            }

            stepList.verticalNormalizedPosition = 1f;
        }

        private void ScrollToFirstActive()
        {
            StepCardView target = cards.FirstOrDefault(card => card.Step.Status == StepStatus.Active).View;
            if (target == null || target == scrolledTo)
            {
                return;
            }

            scrolledTo = target;
            Canvas.ForceUpdateCanvases();
            RectTransform content = stepList.content;
            float scrollable = content.rect.height - stepList.viewport.rect.height;
            if (scrollable <= 0f)
            {
                return;
            }

            var row = (RectTransform)target.transform;
            float rowTop = -row.localPosition.y - row.rect.height * (1f - row.pivot.y);
            float current = (1f - stepList.verticalNormalizedPosition) * scrollable;
            if (rowTop >= current && rowTop + row.rect.height <= current + stepList.viewport.rect.height)
            {
                return;
            }

            float offset = Mathf.Clamp(rowTop - 60f, 0f, scrollable);
            stepList.verticalNormalizedPosition = 1f - offset / scrollable;
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
