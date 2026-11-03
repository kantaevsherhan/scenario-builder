using System.Text;
using ScenarioBuilder.Core;
using ScenarioBuilder.Logging;
using TMPro;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Scrollable view of the current run journal. Updates live while open.</summary>
    public sealed class JournalPanel : MonoBehaviour
    {
        [SerializeField] private ScenarioRunner runner;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text content;
        [SerializeField] private TMP_Text summary;
        [SerializeField] private UnityEngine.UI.ScrollRect scroll;
        [SerializeField] private UnityEngine.UI.Button closeButton;

        private ScenarioSession session;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            runner.SessionStarted += OnSessionStarted;
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            runner.SessionStarted -= OnSessionStarted;
            if (session != null)
            {
                session.EntryWritten -= OnEntryWritten;
            }
        }

        public void Show()
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Render();
        }

        public void Hide() => root.SetActive(false);

        private void OnSessionStarted(ScenarioSession newSession)
        {
            if (session != null)
            {
                session.EntryWritten -= OnEntryWritten;
            }

            session = newSession;
            session.EntryWritten += OnEntryWritten;
        }

        private void OnEntryWritten(JournalEntry entry)
        {
            if (root.activeSelf)
            {
                Render();
            }
        }

        private void Render()
        {
            if (session == null)
            {
                content.text = "Журнал пуст.";
                summary.text = string.Empty;
                return;
            }

            RunJournal journal = session.Journal;
            var text = new StringBuilder();
            foreach (JournalEntry entry in journal.Entries)
            {
                text.AppendLine(Colorize(entry));
            }

            content.text = text.ToString();
            summary.text = $"{journal.ScenarioName} · {journal.ModeName} · записей: {journal.Entries.Count}";
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        private static string Colorize(JournalEntry entry)
        {
            string line = JournalFormatter.Format(entry);
            Color? color = entry.Type switch
            {
                JournalEventTypes.StepViolation or JournalEventTypes.StepFailed => UiPalette.Error,
                JournalEventTypes.StepCompleted => UiPalette.Success,
                JournalEventTypes.StepSkipped => UiPalette.Warning,
                JournalEventTypes.ActionReceived or JournalEventTypes.StepPlanned => UiPalette.Muted,
                _ => null,
            };

            return color.HasValue ? $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{line}</color>" : line;
        }
    }
}
