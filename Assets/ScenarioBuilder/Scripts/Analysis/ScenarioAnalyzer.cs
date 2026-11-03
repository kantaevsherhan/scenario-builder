using System.Collections.Generic;
using ScenarioBuilder.Logging;
using UnityEngine;

namespace ScenarioBuilder.Analysis
{
    /// <summary>
    /// Scores a finished (or interrupted) run from its journal only, so any journal can be
    /// re-analyzed later with any analyzer. Derive from this class to add a new scoring algorithm.
    /// </summary>
    public abstract class ScenarioAnalyzer : ScriptableObject
    {
        [SerializeField, Tooltip("Название анализатора для интерфейса.")]
        private string displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public AnalysisReport Analyze(RunJournal journal)
        {
            var warnings = new List<string>();
            if (journal == null || journal.Entries.Count == 0)
            {
                warnings.Add("Журнал пуст, оценивать нечего.");
                return new AnalysisReport(DisplayName, 0, 0, new List<StepReportLine>(), warnings);
            }

            JournalSummary summary = JournalSummary.From(journal);
            if (!summary.IsFinished)
            {
                warnings.Add("Прохождение не завершено, оценка предварительная.");
            }

            if (!summary.HasPlan)
            {
                warnings.Add("В журнале нет списка шагов (StepPlanned). Учтены только шаги, которые встречаются в записях.");
            }

            return Analyze(summary, warnings);
        }

        /// <param name="warnings">Add diagnostics here instead of throwing on incomplete data.</param>
        protected abstract AnalysisReport Analyze(JournalSummary summary, List<string> warnings);

        internal void SetDisplayName(string value) => displayName = value;
    }
}
