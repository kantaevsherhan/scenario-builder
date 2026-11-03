namespace ScenarioBuilder.Steps
{
    public enum StepEvaluationKind
    {
        /// <summary>The action has nothing to do with the step.</summary>
        Ignored,

        /// <summary>The action moved the step forward but did not finish it.</summary>
        Progressed,

        Completed,
        Violation,
    }

    /// <summary>How a step reacted to a single action.</summary>
    public readonly struct StepEvaluation
    {
        private StepEvaluation(StepEvaluationKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }

        public StepEvaluationKind Kind { get; }

        /// <summary>Human readable reason. Filled for violations.</summary>
        public string Message { get; }

        public static StepEvaluation Ignored => new(StepEvaluationKind.Ignored, string.Empty);
        public static StepEvaluation Progressed => new(StepEvaluationKind.Progressed, string.Empty);
        public static StepEvaluation Completed => new(StepEvaluationKind.Completed, string.Empty);

        public static StepEvaluation Violation(string message) => new(StepEvaluationKind.Violation, message);
    }
}
