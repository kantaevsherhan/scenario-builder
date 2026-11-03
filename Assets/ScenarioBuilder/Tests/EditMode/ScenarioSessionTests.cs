using System.Linq;
using NUnit.Framework;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Core;
using ScenarioBuilder.Logging;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using static ScenarioBuilder.Tests.TestContent;

namespace ScenarioBuilder.Tests
{
    public sealed class ScenarioSessionTests
    {
        private TestContent content;
        private ActionDefinition[] actions;
        private ActionStep[] steps;

        [SetUp]
        public void SetUp()
        {
            content = new TestContent();
            actions = Enumerable.Range(1, 12).Select(i => content.Action($"a{i}")).ToArray();
            steps = actions.Select((action, i) => content.Step($"s{i + 1}", action)).ToArray();
        }

        [TearDown]
        public void TearDown() => content.Dispose();

        [Test]
        public void MixedActivation_OpensStagesInOrder()
        {
            ScenarioSession session = Start(content.Training(MixedOrder()));

            AssertActive(session, "s1", "s2", "s3", "s4");

            Perform(session, 3, 1, 4);
            AssertActive(session, "s2");

            Perform(session, 2);
            AssertActive(session, "s5");

            Perform(session, 5, 6, 7);
            AssertActive(session, "s8");

            Perform(session, 8);
            AssertActive(session, "s9", "s10");

            Perform(session, 10, 9);
            AssertActive(session, "s11", "s12");

            Perform(session, 12, 11);
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.Steps.All(step => step.Status == StepStatus.Completed), Is.True);
        }

        [Test]
        public void Training_Violation_KeepsStepActiveAndCountsAttempt()
        {
            ActionDefinition wrong = content.Action("wrong");
            ActionStep guarded = content.Step("guarded", new[] { new RequiredAction(actions[0]) },
                new[] { new ForbiddenAction(wrong) });
            ScenarioSession session = Start(content.Training(After(guarded), With(steps[1])));

            session.HandleAction(wrong);

            StepState state = session.Steps[0];
            Assert.That(state.Status, Is.EqualTo(StepStatus.Active));
            Assert.That(state.Attempt, Is.EqualTo(2));
            AssertActive(session, "guarded", "s2");
        }

        [Test]
        public void OutOfTurnAction_IsViolationOfWaitingStep()
        {
            ScenarioSession session = Start(content.Training(After(steps[0]), After(steps[1])));

            session.HandleAction(actions[1]);

            Assert.That(session.Steps[1].Status, Is.EqualTo(StepStatus.Pending));
            Assert.That(session.Steps[1].Violations, Is.EqualTo(1));
            Assert.That(session.Journal.Entries.Count(entry => entry.Type == JournalEventTypes.StepViolation), Is.EqualTo(1));
        }

        [Test]
        public void OutOfTurnAction_IsIgnored_WhenPolicyIsIgnore()
        {
            ScenarioSession session = Start(content.Training(OutOfTurnPolicy.Ignore, After(steps[0]), After(steps[1])));

            session.HandleAction(actions[1]);
            Perform(session, 1);

            Assert.That(session.Steps[1].Violations, Is.Zero);
            AssertActive(session, "s2");
        }

        [Test]
        public void Exam_Violation_FailsStepWithoutRetry()
        {
            ActionDefinition wrong = content.Action("wrong");
            ActionStep guarded = content.Step("guarded", new[] { new RequiredAction(actions[0]) },
                new[] { new ForbiddenAction(wrong) });
            ScenarioSession session = Start(content.Exam(2, new StepGroup("G1", new[] { After(guarded), After(steps[1]) })));

            session.HandleAction(wrong);

            Assert.That(session.Steps[0].Status, Is.EqualTo(StepStatus.Failed));
            AssertActive(session, "s2");
        }

        [Test]
        public void Exam_ThresholdReached_SkipsRestOfGroupAndStartsNext()
        {
            var group1 = new StepGroup("G1", new[] { After(steps[0]), After(steps[1]), After(steps[2]) });
            var group2 = new StepGroup("G2", new[] { After(steps[3]) });
            ScenarioSession session = Start(content.Exam(2, group1, group2));

            Perform(session, 2, 3);

            Assert.That(session.Steps.Take(3).Select(step => step.Status), Is.EqualTo(new[]
            {
                StepStatus.Skipped, StepStatus.Failed, StepStatus.Failed,
            }));
            Assert.That(session.CurrentGroupName, Is.EqualTo("G2"));
            AssertActive(session, "s4");
        }

        private ActivationEntry[] MixedOrder() => new[]
        {
            After(steps[0]), With(steps[1]), With(steps[2]), With(steps[3]),
            After(steps[4]), After(steps[5]), After(steps[6]), After(steps[7]),
            After(steps[8]), With(steps[9]),
            After(steps[10]), With(steps[11]),
        };

        private void Perform(ScenarioSession session, params int[] actionNumbers)
        {
            foreach (int number in actionNumbers)
            {
                session.HandleAction(actions[number - 1]);
            }
        }

        private static void AssertActive(ScenarioSession session, params string[] stepIds) =>
            Assert.That(session.ActiveSteps.Select(step => step.Id), Is.EquivalentTo(stepIds));
    }
}
