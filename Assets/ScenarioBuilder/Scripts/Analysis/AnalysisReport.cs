using System.Collections.Generic;

namespace ScenarioBuilder.Analysis
{
    /// <summary>Score line of a single step.</summary>
    public sealed class StepReportLine
    {
        public StepReportLine(string stepId, string title, StepResult result, int attempts, int points)
        {
            StepId = stepId;
            Title = title;
            Result = result;
            Attempts = attempts;
            Points = points;
        }

        public string StepId { get; }
        public string Title { get; }
        public StepResult Result { get; }
        public int Attempts { get; }

        /// <summary>Points earned (positive) or penalty (negative).</summary>
        public int Points { get; }
    }

    /// <summary>Result of analyzing one run journal.</summary>
    public sealed class AnalysisReport
    {
        public AnalysisReport(string analyzerName, int score, int maxScore,
            IReadOnlyList<StepReportLine> lines, IReadOnlyList<string> warnings)
        {
            AnalyzerName = analyzerName;
            Score = score;
            MaxScore = maxScore;
            Lines = lines;
            Warnings = warnings;
        }

        public string AnalyzerName { get; }
        public int Score { get; }
        public int MaxScore { get; }
        public IReadOnlyList<StepReportLine> Lines { get; }

        /// <summary>Diagnostics about missing or unexpected data. A report with warnings is still usable.</summary>
        public IReadOnlyList<string> Warnings { get; }
    }
}
