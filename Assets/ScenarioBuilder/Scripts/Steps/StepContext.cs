using ScenarioBuilder.Core;

namespace ScenarioBuilder.Steps
{
    /// <summary>
    /// Run-time data of one step during one run. Step assets stay read-only,
    /// everything that changes while the user works on a step lives here.
    /// </summary>
    public sealed class StepContext
    {
        public StepContext(ScenarioWorldState worldState)
        {
            WorldState = worldState;
        }

        public ScenarioWorldState WorldState { get; }

        /// <summary>Generic progress counter, e.g. how many actions of a sequence are done.</summary>
        public int Progress { get; set; }

        public void ResetProgress() => Progress = 0;
    }
}
