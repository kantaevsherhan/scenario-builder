using System.Linq;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Final report with per-step lines and buttons to re-score the same run with another analyzer.</summary>
    public sealed class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private ScenarioRunner runner;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text analyzerText;
        [SerializeField] private TMP_Text warningsText;
        [SerializeField] private TMP_Text lineTemplate;
        [SerializeField] private UnityEngine.UI.Button analyzerButtonTemplate;
        [SerializeField] private UnityEngine.UI.Button journalButton;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private JournalPanel journalPanel;

        private TemplateList<TMP_Text> lines;
        private TemplateList<UnityEngine.UI.Button> analyzerButtons;

        private void Awake()
        {
            lines = new TemplateList<TMP_Text>(lineTemplate);
            analyzerButtons = new TemplateList<UnityEngine.UI.Button>(analyzerButtonTemplate);
            journalButton.onClick.AddListener(journalPanel.Show);
            restartButton.onClick.AddListener(runner.RestartRun);
            closeButton.onClick.AddListener(Hide);
            runner.ReportReady += Show;
            runner.SessionStarted += OnSessionStarted;
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            runner.ReportReady -= Show;
            runner.SessionStarted -= OnSessionStarted;
        }

        private void OnSessionStarted(ScenarioSession session) => Hide();

        public void Hide() => root.SetActive(false);

        public void ShowLastReport()
        {
            if (runner.LastReport != null)
            {
                Show(runner.LastReport);
            }
        }

        private void Show(AnalysisReport report)
        {
            root.SetActive(true);
            scoreText.text = $"{report.Score} / {report.MaxScore}";
            analyzerText.text = $"{report.AnalyzerName} · режим «{runner.Session.Mode.DisplayName}»";
            warningsText.text = string.Join("\n", report.Warnings.Select(warning => $"• {warning}"));
            warningsText.gameObject.SetActive(report.Warnings.Count > 0);

            lines.Clear();
            foreach (StepReportLine line in report.Lines)
            {
                string color = ColorUtility.ToHtmlStringRGB(UiPalette.For(line.Result));
                string points = line.Points == 0 ? "0" : line.Points.ToString("+0;-0");
                lines.Add().text = $"<color=#{color}>■</color> {line.Title}" +
                                   $"<pos=72%><color=#{color}>{UiPalette.Label(line.Result)}</color>" +
                                   $"<pos=88%>{points}";
            }

            RebuildAnalyzerButtons(report);
        }

        private void RebuildAnalyzerButtons(AnalysisReport current)
        {
            analyzerButtons.Clear();
            foreach (ScenarioAnalyzer analyzer in runner.Analyzers.Where(analyzer => analyzer != null))
            {
                UnityEngine.UI.Button button = analyzerButtons.Add();
                button.GetComponentInChildren<TMP_Text>().text = analyzer.DisplayName;
                button.interactable = analyzer.DisplayName != current.AnalyzerName;
                button.onClick.AddListener(() => runner.Analyze(analyzer));
            }
        }
    }
}
