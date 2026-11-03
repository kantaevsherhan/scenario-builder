using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;

namespace ScenarioBuilder.Tests
{
    /// <summary>The demo scenario assets match the specification: modes, groups, activation, threshold, context steps.</summary>
    public sealed class DemoAssetsTests
    {
        private const string ScenarioPath = "Assets/ScenarioBuilder/Demo/Data/DemoScenario.asset";

        private ScenarioDefinition scenario;
        private TrainingMode training;
        private ExamMode exam;

        [SetUp]
        public void SetUp()
        {
            scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ScenarioPath);
            Assert.That(scenario, Is.Not.Null, ScenarioPath);
            training = scenario.Modes.OfType<TrainingMode>().Single();
            exam = scenario.Modes.OfType<ExamMode>().Single();
        }

        [Test]
        public void Scenario_HasTwelveStepsAndNoConfigurationIssues()
        {
            Assert.That(scenario.Steps.Count, Is.GreaterThanOrEqualTo(12));
            Assert.That(ScenarioValidator.Validate(scenario), Is.Empty);
        }

        [Test]
        public void Training_UsesMixedActivationFromSpec()
        {
            List<List<StepDefinition>> stages = ActivationPlan.BuildStages(training.Groups.Single().Steps);

            Assert.That(stages.Select(stage => stage.Select(Number).ToArray()), Is.EqualTo(new[]
            {
                new[] { 1, 2, 3, 4 }, new[] { 5 }, new[] { 6 }, new[] { 7 }, new[] { 8 }, new[] { 9, 10 }, new[] { 11, 12 },
            }));
        }

        [Test]
        public void Exam_HasThreeGroupsOfFour_AndThresholdTwo()
        {
            Assert.That(exam.Groups.Count, Is.EqualTo(3));
            Assert.That(exam.Groups.Select(group => group.Steps.Count), Is.All.EqualTo(4));
            Assert.That(exam.ViolationThreshold, Is.EqualTo(2));
        }

        [Test]
        public void BothModes_ReuseTheSameStepAssets()
        {
            IEnumerable<StepDefinition> StepsOf(ScenarioMode mode) => mode.Groups.SelectMany(group => group.Steps).Select(entry => entry.Step);

            Assert.That(StepsOf(training), Is.EquivalentTo(scenario.Steps));
            Assert.That(StepsOf(exam), Is.EquivalentTo(scenario.Steps));
        }

        [Test]
        public void Modes_ScoreWithTheirAlgorithms_AndTreatOutOfTurnAsViolation()
        {
            Assert.That(training.DefaultAnalyzer, Is.InstanceOf<AttemptScoreAnalyzer>());
            Assert.That(exam.DefaultAnalyzer, Is.InstanceOf<PenaltyScoreAnalyzer>());
            Assert.That(scenario.Modes.Select(mode => mode.OutOfTurnPolicy), Is.All.EqualTo(OutOfTurnPolicy.Violation));
        }

        [Test]
        public void AtLeastTwoSteps_HaveContextLogic()
        {
            var emptyWorld = new ScenarioWorldState();
            List<ActionStep> composite = scenario.Steps.OfType<ActionStep>()
                .Where(step => step.RequiredActions.Count > 1).ToList();
            List<ActionStep> stateDependent = scenario.Steps.OfType<ActionStep>()
                .Where(step => step.RequiredActions.Any(action => action.TryFindUnmetRequirement(emptyWorld, out _))).ToList();

            Assert.That(composite, Is.Not.Empty, "A step like «zone A, then zone B».");
            Assert.That(stateDependent, Is.Not.Empty, "A step that needs another object's state.");
            Assert.That(composite.Concat(stateDependent).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
        }

        private int Number(StepDefinition step) => scenario.Steps.ToList().IndexOf(step) + 1;
    }
}
