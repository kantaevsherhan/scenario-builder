using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Logging;

namespace ScenarioBuilder.Analysis
{
    public enum StepResult
    {
        Unfinished,
        Completed,
        Failed,
        Skipped,
    }

    public sealed class StepOutcome
    {
        public StepOutcome(string stepId)
        {
            StepId = stepId;
            Title = stepId;
        }

        public string StepId { get; }
        public string Title { get; internal set; }
        public string GroupName { get; internal set; }
        public StepResult Result { get; internal set; }
        public int Violations { get; internal set; }

        /// <summary>Attempt number recorded on completion. Null when the journal has no such field.</summary>
        public int? RecordedAttempt { get; internal set; }
    }

    /// <summary>Reduces a run journal to per-step facts. Shared by all analyzers; unknown entry types are ignored.</summary>
    public sealed class JournalSummary
    {
        private JournalSummary(IReadOnlyList<StepOutcome> steps, bool hasPlan, bool isFinished)
        {
            Steps = steps;
            HasPlan = hasPlan;
            IsFinished = isFinished;
        }

        /// <summary>Steps in the order they first appear in the journal.</summary>
        public IReadOnlyList<StepOutcome> Steps { get; }

        /// <summary>True when the journal lists every planned step, not only the ones that were reached.</summary>
        public bool HasPlan { get; }

        public bool IsFinished { get; }

        public static JournalSummary From(RunJournal journal)
        {
            var steps = new Dictionary<string, StepOutcome>();
            var order = new List<StepOutcome>();
            bool hasPlan = false;
            bool isFinished = false;

            foreach (JournalEntry entry in journal.Entries)
            {
                hasPlan |= entry.Type == JournalEventTypes.StepPlanned;
                isFinished |= entry.Type == JournalEventTypes.RunFinished;
                if (!entry.HasStep)
                {
                    continue;
                }

                if (!steps.TryGetValue(entry.StepId, out StepOutcome step))
                {
                    step = new StepOutcome(entry.StepId);
                    steps.Add(entry.StepId, step);
                    order.Add(step);
                }

                Apply(entry, step);
            }

            return new JournalSummary(order, hasPlan, isFinished);
        }

        public int Count(StepResult result) => Steps.Count(step => step.Result == result);

        private static void Apply(JournalEntry entry, StepOutcome step)
        {
            if (!string.IsNullOrEmpty(entry.GroupName))
            {
                step.GroupName = entry.GroupName;
            }

            switch (entry.Type)
            {
                case JournalEventTypes.StepPlanned:
                case JournalEventTypes.StepActivated:
                    if (!string.IsNullOrEmpty(entry.Message))
                    {
                        step.Title = entry.Message;
                    }

                    break;
                case JournalEventTypes.StepViolation:
                    step.Violations++;
                    break;
                case JournalEventTypes.StepCompleted:
                    step.Result = StepResult.Completed;
                    step.RecordedAttempt = entry.TryGetInt(JournalFieldKeys.Attempt, out int attempt) ? attempt : null;
                    break;
                case JournalEventTypes.StepFailed:
                    step.Result = StepResult.Failed;
                    break;
                case JournalEventTypes.StepSkipped:
                    step.Result = StepResult.Skipped;
                    break;
            }
        }
    }
}
