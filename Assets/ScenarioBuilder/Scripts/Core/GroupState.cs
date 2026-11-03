using System.Collections.Generic;
using System.Linq;

namespace ScenarioBuilder.Core
{
    /// <summary>Run-time state of one step group: its stages and violation counter.</summary>
    internal sealed class GroupState
    {
        public GroupState(string name, List<List<StepState>> stages)
        {
            Name = name;
            Stages = stages;
        }

        public string Name { get; }
        public List<List<StepState>> Stages { get; }
        public int StageIndex { get; private set; }
        public int Violations { get; set; }

        public IEnumerable<StepState> Steps => Stages.SelectMany(stage => stage);

        public bool IsCurrentStageFinished => StageIndex >= Stages.Count || Stages[StageIndex].All(step => step.IsFinished);

        public bool HasNextStage => StageIndex + 1 < Stages.Count;

        public IReadOnlyList<StepState> CurrentStage =>
            StageIndex < Stages.Count ? Stages[StageIndex] : new List<StepState>();

        public void MoveToNextStage() => StageIndex++;
    }
}
