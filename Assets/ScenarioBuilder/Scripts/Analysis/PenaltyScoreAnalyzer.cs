using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder.Analysis
{
    /// <summary>
    /// Exam scoring: starts from the base score and subtracts a penalty for every failed and skipped step.
    /// Score = max(0, base − penalties).
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Analyzers/Penalty Score Analyzer", fileName = "PenaltyScoreAnalyzer", order = 51)]
    public sealed class PenaltyScoreAnalyzer : ScenarioAnalyzer
    {
        [SerializeField, Min(0)] private int baseScore = 100;
        [SerializeField, Min(0)] private int failedPenalty = 10;
        [SerializeField, Min(0)] private int skippedPenalty = 5;

        protected override AnalysisReport Analyze(JournalSummary summary, List<string> warnings)
        {
            var lines = new List<StepReportLine>();
            int penalties = 0;

            foreach (StepOutcome step in summary.Steps)
            {
                int penalty = step.Result switch
                {
                    StepResult.Failed => failedPenalty,
                    StepResult.Skipped => skippedPenalty,
                    _ => 0,
                };

                penalties += penalty;
                lines.Add(new StepReportLine(step.StepId, step.Title, step.Result, step.Violations + 1, -penalty));
            }

            if (summary.Steps.Any(step => step.Result == StepResult.Completed && step.Violations > 0))
            {
                warnings.Add("Есть шаги, выполненные после повторных попыток (тренировочный прогон) — " +
                             "штрафуются только статусы Failed и Skipped.");
            }

            int unfinished = summary.Count(StepResult.Unfinished);
            if (unfinished > 0)
            {
                warnings.Add($"Шагов без итогового статуса: {unfinished} — штраф за них не начислен.");
            }

            return new AnalysisReport(DisplayName, Mathf.Max(0, baseScore - penalties), baseScore, lines, warnings);
        }
    }
}
