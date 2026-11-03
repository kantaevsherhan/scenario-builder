using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder.Analysis
{
    /// <summary>
    /// Training scoring: points per completed step depend on the attempt it was completed with.
    /// Maximum = points for the first attempt × number of steps.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Analyzers/Attempt Score Analyzer", fileName = "AttemptScoreAnalyzer", order = 50)]
    public sealed class AttemptScoreAnalyzer : ScenarioAnalyzer
    {
        [SerializeField, Min(0)] private int firstAttemptPoints = 10;
        [SerializeField, Min(0)] private int secondAttemptPoints = 5;
        [SerializeField, Min(0), Tooltip("Баллы за выполнение с третьей и последующих попыток.")]
        private int laterAttemptPoints;

        protected override AnalysisReport Analyze(JournalSummary summary, List<string> warnings)
        {
            var lines = new List<StepReportLine>();
            bool attemptsWereDerived = false;
            int score = 0;

            foreach (StepOutcome step in summary.Steps)
            {
                int attempts = step.RecordedAttempt ?? step.Violations + 1;
                attemptsWereDerived |= step.Result == StepResult.Completed && step.RecordedAttempt == null;

                int points = step.Result == StepResult.Completed ? PointsForAttempt(attempts) : 0;
                score += points;
                lines.Add(new StepReportLine(step.StepId, step.Title, step.Result, attempts, points));
            }

            if (attemptsWereDerived)
            {
                warnings.Add("В записях о завершении нет номера попытки — попытки вычислены по числу нарушений.");
            }

            if (summary.Count(StepResult.Failed) + summary.Count(StepResult.Skipped) > 0)
            {
                warnings.Add("В журнале есть проваленные или пропущенные шаги (экзаменационный прогон) — за них 0 баллов.");
            }

            int maxScore = firstAttemptPoints * summary.Steps.Count;
            return new AnalysisReport(DisplayName, score, maxScore, lines, warnings);
        }

        private int PointsForAttempt(int attempt) => attempt switch
        {
            1 => firstAttemptPoints,
            2 => secondAttemptPoints,
            _ => laterAttemptPoints,
        };
    }
}
