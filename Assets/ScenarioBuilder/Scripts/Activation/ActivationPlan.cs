using System.Collections.Generic;
using ScenarioBuilder.Steps;

namespace ScenarioBuilder.Activation
{
    /// <summary>Turns a flat activation list into stages of simultaneously active steps.</summary>
    public static class ActivationPlan
    {
        /// <summary>
        /// Every "after previous" entry starts a new stage, every "with previous" entry joins the current one.
        /// Stage N+1 opens when all steps of stage N are finished.
        /// </summary>
        public static List<List<StepDefinition>> BuildStages(IEnumerable<ActivationEntry> entries)
        {
            var stages = new List<List<StepDefinition>>();
            foreach (ActivationEntry entry in entries)
            {
                if (entry?.Step == null)
                {
                    continue;
                }

                bool startsNewStage = entry.Activation == ActivationMode.AfterPrevious || stages.Count == 0;
                if (startsNewStage)
                {
                    stages.Add(new List<StepDefinition>());
                }

                stages[^1].Add(entry.Step);
            }

            return stages;
        }
    }
}
