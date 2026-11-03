using ScenarioBuilder.Steps;

namespace ScenarioBuilder.Core
{
    public enum StepStatus
    {
        /// <summary>Waiting for its turn.</summary>
        Pending,
        Active,
        Completed,
        Failed,
        Skipped,
    }

    /// <summary>Run-time state of one step within one run.</summary>
    public sealed class StepState
    {
        internal StepState(StepDefinition definition, string groupName, ScenarioWorldState worldState)
        {
            Definition = definition;
            GroupName = groupName;
            Context = new StepContext(worldState);
        }

        public StepDefinition Definition { get; }
        public string Id => Definition.Id;
        public string GroupName { get; }
        public StepStatus Status { get; internal set; }

        /// <summary>Current attempt number, starting from 1.</summary>
        public int Attempt { get; internal set; } = 1;

        public int Violations { get; internal set; }

        public bool IsFinished => Status is StepStatus.Completed or StepStatus.Failed or StepStatus.Skipped;

        internal StepContext Context { get; }
    }
}
