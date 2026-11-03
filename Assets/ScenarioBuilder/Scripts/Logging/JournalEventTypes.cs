namespace ScenarioBuilder.Logging
{
    /// <summary>
    /// Entry types written by the scenario session. Types are plain strings,
    /// so custom loggers and steps can add their own without touching this list.
    /// </summary>
    public static class JournalEventTypes
    {
        public const string RunStarted = "RunStarted";
        public const string RunFinished = "RunFinished";
        public const string RunAborted = "RunAborted";

        /// <summary>One per step at run start: the full plan of the run with step titles.</summary>
        public const string StepPlanned = "StepPlanned";

        public const string GroupStarted = "GroupStarted";
        public const string GroupFinished = "GroupFinished";
        public const string ActionReceived = "ActionReceived";
        public const string StepActivated = "StepActivated";
        public const string StepProgressed = "StepProgressed";
        public const string StepViolation = "StepViolation";
        public const string StepCompleted = "StepCompleted";
        public const string StepFailed = "StepFailed";
        public const string StepSkipped = "StepSkipped";
    }

    /// <summary>Values of <see cref="JournalEntry.Result"/>.</summary>
    public static class JournalResults
    {
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
        public const string Violation = "Violation";
    }

    /// <summary>Keys of optional entry fields.</summary>
    public static class JournalFieldKeys
    {
        public const string Attempt = "attempt";
        public const string OutOfTurn = "outOfTurn";
        public const string StepCount = "stepCount";
        public const string GroupCount = "groupCount";
        public const string Violations = "violations";
        public const string Action = "action";
    }
}
