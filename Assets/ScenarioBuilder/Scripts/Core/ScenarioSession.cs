using System;
using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Logging;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEngine;

namespace ScenarioBuilder.Core
{
    /// <summary>
    /// One run of a scenario in a given mode. Plain C# object: receives actions,
    /// activates steps stage by stage, applies the mode's violation rules and writes the journal.
    /// </summary>
    public sealed class ScenarioSession
    {
        private readonly ScenarioMode mode;
        private readonly List<GroupState> groups;
        private readonly List<StepState> steps;
        private readonly Func<float> clock;
        private float startTime;
        private int groupIndex;

        public ScenarioSession(string scenarioName, ScenarioMode mode, ScenarioWorldState worldState, Func<float> clock)
        {
            this.mode = mode;
            this.clock = clock;
            Journal = new RunJournal(scenarioName, mode.DisplayName, DateTime.UtcNow);
            groups = BuildGroups(mode, worldState);
            steps = groups.SelectMany(group => group.Steps).ToList();
        }

        /// <summary>A step was activated, moved forward, finished or got a new attempt.</summary>
        public event Action<StepState> StepChanged;

        /// <summary>Raised after the mode has decided what happens to the step, so its status is final.</summary>
        public event Action<StepState, string> ViolationRaised;
        public event Action<JournalEntry> EntryWritten;
        public event Action Finished;

        public ScenarioMode Mode => mode;
        public RunJournal Journal { get; }
        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public IReadOnlyList<StepState> Steps => steps;
        public IEnumerable<StepState> ActiveSteps => steps.Where(step => step.Status == StepStatus.Active);
        public int GroupCount => groups.Count;

        /// <summary>1-based number of the current group.</summary>
        public int CurrentGroupNumber => groupIndex + 1;

        public string CurrentGroupName => groups.Count > 0 ? CurrentGroup.Name : string.Empty;

        private GroupState CurrentGroup => groups[groupIndex];
        private float Elapsed => clock() - startTime;

        public void Start()
        {
            if (IsRunning || IsFinished)
            {
                throw new InvalidOperationException("A session can be started only once.");
            }

            startTime = clock();
            IsRunning = true;
            Write(new JournalEntry(Elapsed, JournalEventTypes.RunStarted, message: mode.DisplayName)
                .With(JournalFieldKeys.StepCount, steps.Count)
                .With(JournalFieldKeys.GroupCount, groups.Count));

            foreach (StepState step in steps)
            {
                Write(StepEntry(JournalEventTypes.StepPlanned, step, message: step.Definition.Instruction));
            }

            if (groups.Count == 0)
            {
                FinishRun();
                return;
            }

            StartCurrentGroup();
            AdvanceFinishedStages();
        }

        public void HandleAction(ActionDefinition action)
        {
            if (!IsRunning || action == null)
            {
                return;
            }

            Write(new JournalEntry(Elapsed, JournalEventTypes.ActionReceived, groupName: CurrentGroup.Name,
                message: action.DisplayName).With(JournalFieldKeys.Action, action.name));

            GroupState group = CurrentGroup;
            bool handled = false;
            foreach (StepState step in group.CurrentStage.ToList())
            {
                if (IsRunning && step.Status == StepStatus.Active)
                {
                    handled |= Evaluate(step, action);
                }
            }

            bool canBeOutOfTurn = !handled && IsRunning && group == CurrentGroup &&
                                  mode.OutOfTurnPolicy == OutOfTurnPolicy.Violation;
            if (canBeOutOfTurn)
            {
                StepState waitingStep = group.Steps.FirstOrDefault(step =>
                    step.Status == StepStatus.Pending && step.Definition.Involves(action));
                if (waitingStep != null)
                {
                    RegisterViolation(waitingStep,
                        $"Действие вне очереди: шаг «{waitingStep.Definition.Instruction}» ещё не доступен", true);
                }
            }

            AdvanceFinishedStages();
        }

        /// <summary>Stops an unfinished run, e.g. on restart. The journal stays available.</summary>
        public void Abort()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            Write(new JournalEntry(Elapsed, JournalEventTypes.RunAborted));
        }

        private static List<GroupState> BuildGroups(ScenarioMode mode, ScenarioWorldState worldState)
        {
            var usedSteps = new HashSet<StepDefinition>();
            var result = new List<GroupState>();
            foreach (StepGroup group in mode.Groups)
            {
                int groupIndex = result.Count;
                var stages = new List<List<StepState>>();
                foreach (List<StepDefinition> stage in ActivationPlan.BuildStages(group.Steps))
                {
                    List<StepState> stageSteps = stage
                        .Where(step => WarnIfDuplicate(usedSteps.Add(step), step, mode))
                        .Select(step => new StepState(step, group.Name, groupIndex, worldState))
                        .ToList();
                    if (stageSteps.Count > 0)
                    {
                        stages.Add(stageSteps);
                    }
                }

                result.Add(new GroupState(group.Name, stages));
            }

            return result;
        }

        private static bool WarnIfDuplicate(bool isFirstUse, StepDefinition step, ScenarioMode mode)
        {
            if (!isFirstUse)
            {
                Debug.LogWarning($"Шаг «{step.Id}» указан в режиме «{mode.DisplayName}» несколько раз — повтор пропущен.", mode);
            }

            return isFirstUse;
        }

        private bool Evaluate(StepState step, ActionDefinition action)
        {
            StepEvaluation evaluation = step.Definition.Evaluate(action, step.Context);
            switch (evaluation.Kind)
            {
                case StepEvaluationKind.Progressed:
                    Write(StepEntry(JournalEventTypes.StepProgressed, step, message: action.DisplayName)
                        .With(JournalFieldKeys.Attempt, step.Attempt));
                    StepChanged?.Invoke(step);
                    return true;
                case StepEvaluationKind.Completed:
                    FinishStep(step, StepStatus.Completed);
                    return true;
                case StepEvaluationKind.Violation:
                    RegisterViolation(step, evaluation.Message, false);
                    return true;
                default:
                    return false;
            }
        }

        private void RegisterViolation(StepState step, string message, bool isOutOfTurn)
        {
            GroupState group = CurrentGroup;
            group.Violations++;
            step.Violations++;
            Write(StepEntry(JournalEventTypes.StepViolation, step, JournalResults.Violation, message)
                .With(JournalFieldKeys.Attempt, step.Attempt)
                .With(JournalFieldKeys.OutOfTurn, isOutOfTurn));

            switch (mode.ResolveViolation(group.Violations))
            {
                case ViolationResponse.RetryStep:
                    step.Attempt++;
                    step.Context.ResetProgress();
                    StepChanged?.Invoke(step);
                    break;
                case ViolationResponse.FailStep:
                    FinishStep(step, StepStatus.Failed);
                    break;
                case ViolationResponse.FailStepAndSkipGroup:
                    FinishStep(step, StepStatus.Failed);
                    foreach (StepState unfinished in group.Steps.Where(item => !item.IsFinished).ToList())
                    {
                        FinishStep(unfinished, StepStatus.Skipped);
                    }

                    break;
            }

            ViolationRaised?.Invoke(step, message);
        }

        private void FinishStep(StepState step, StepStatus status)
        {
            step.Status = status;
            (string type, string result) = status switch
            {
                StepStatus.Completed => (JournalEventTypes.StepCompleted, JournalResults.Completed),
                StepStatus.Failed => (JournalEventTypes.StepFailed, JournalResults.Failed),
                StepStatus.Skipped => (JournalEventTypes.StepSkipped, JournalResults.Skipped),
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Not a final status."),
            };

            Write(StepEntry(type, step, result).With(JournalFieldKeys.Attempt, step.Attempt));
            StepChanged?.Invoke(step);
        }

        private void AdvanceFinishedStages()
        {
            while (IsRunning && CurrentGroup.IsCurrentStageFinished)
            {
                if (CurrentGroup.HasNextStage)
                {
                    CurrentGroup.MoveToNextStage();
                    ActivateCurrentStage();
                    continue;
                }

                Write(new JournalEntry(Elapsed, JournalEventTypes.GroupFinished, groupName: CurrentGroup.Name)
                    .With(JournalFieldKeys.Violations, CurrentGroup.Violations));

                if (groupIndex + 1 < groups.Count)
                {
                    groupIndex++;
                    StartCurrentGroup();
                }
                else
                {
                    FinishRun();
                }
            }
        }

        private void StartCurrentGroup()
        {
            Write(new JournalEntry(Elapsed, JournalEventTypes.GroupStarted, groupName: CurrentGroup.Name));
            ActivateCurrentStage();
        }

        private void ActivateCurrentStage()
        {
            foreach (StepState step in CurrentGroup.CurrentStage.Where(step => step.Status == StepStatus.Pending))
            {
                step.Status = StepStatus.Active;
                Write(StepEntry(JournalEventTypes.StepActivated, step, message: step.Definition.Instruction));
                StepChanged?.Invoke(step);
            }
        }

        private void FinishRun()
        {
            IsRunning = false;
            IsFinished = true;
            Write(new JournalEntry(Elapsed, JournalEventTypes.RunFinished));
            Finished?.Invoke();
        }

        private JournalEntry StepEntry(string type, StepState step, string result = "", string message = "") =>
            new(Elapsed, type, step.Id, step.GroupName, result, message);

        private void Write(JournalEntry entry)
        {
            Journal.Add(entry);
            EntryWritten?.Invoke(entry);
        }
    }
}
