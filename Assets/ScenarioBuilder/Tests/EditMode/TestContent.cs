using System;
using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScenarioBuilder.Tests
{
    /// <summary>Creates in-memory scenario assets for a test and destroys them afterwards.</summary>
    internal sealed class TestContent : IDisposable
    {
        private readonly List<Object> created = new();

        public static ActivationEntry After(StepDefinition step) => new(step, ActivationMode.AfterPrevious);

        public static ActivationEntry With(StepDefinition step) => new(step, ActivationMode.WithPrevious);

        public ActionDefinition Action(string name) => Create<ActionDefinition>(name);

        public StateFlag Flag(string name) => Create<StateFlag>(name);

        /// <summary>A step completed by the given actions in order.</summary>
        public ActionStep Step(string id, params ActionDefinition[] sequence) =>
            Step(id, sequence.Select(action => new RequiredAction(action)));

        public ActionStep Step(string id, IEnumerable<RequiredAction> required,
            IEnumerable<ForbiddenAction> forbidden = null, bool wrongOrderIsViolation = true)
        {
            var step = Create<ActionStep>(id);
            step.Setup(id, $"Instruction {id}", required, forbidden, wrongOrderIsViolation);
            return step;
        }

        public TrainingMode Training(params ActivationEntry[] order) => Training(OutOfTurnPolicy.Violation, order);

        public TrainingMode Training(OutOfTurnPolicy policy, params ActivationEntry[] order)
        {
            var mode = Create<TrainingMode>("Training");
            mode.SetCommon("Training", policy, null);
            mode.Setup(order);
            return mode;
        }

        public ExamMode Exam(int threshold, params StepGroup[] groups)
        {
            var mode = Create<ExamMode>("Exam");
            mode.SetCommon("Exam", OutOfTurnPolicy.Violation, null);
            mode.Setup(threshold, groups);
            return mode;
        }

        public ScenarioDefinition Scenario(StepDefinition[] steps, params ScenarioMode[] modes)
        {
            var scenario = Create<ScenarioDefinition>("Scenario");
            scenario.Setup("Test scenario", steps, modes);
            return scenario;
        }

        public AttemptScoreAnalyzer AttemptAnalyzer() => Create<AttemptScoreAnalyzer>("AttemptScoreAnalyzer");

        public PenaltyScoreAnalyzer PenaltyAnalyzer() => Create<PenaltyScoreAnalyzer>("PenaltyScoreAnalyzer");

        public static ScenarioSession Start(ScenarioMode mode, ScenarioWorldState worldState = null)
        {
            var session = new ScenarioSession("Test scenario", mode, worldState ?? new ScenarioWorldState(), () => 0f);
            session.Start();
            return session;
        }

        public void Dispose()
        {
            foreach (Object asset in created)
            {
                Object.DestroyImmediate(asset);
            }
        }

        private T Create<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            created.Add(asset);
            return asset;
        }
    }
}
