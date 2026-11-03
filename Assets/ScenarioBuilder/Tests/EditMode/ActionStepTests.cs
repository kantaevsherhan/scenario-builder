using NUnit.Framework;
using ScenarioBuilder.Core;
using ScenarioBuilder.Steps;

namespace ScenarioBuilder.Tests
{
    public sealed class ActionStepTests
    {
        private TestContent content;
        private ScenarioWorldState worldState;
        private StepContext context;

        [SetUp]
        public void SetUp()
        {
            content = new TestContent();
            worldState = new ScenarioWorldState();
            context = new StepContext(worldState);
        }

        [TearDown]
        public void TearDown() => content.Dispose();

        [Test]
        public void Sequence_InOrder_ProgressesThenCompletes()
        {
            ActionDefinition zoneA = content.Action("ZoneA");
            ActionDefinition zoneB = content.Action("ZoneB");
            ActionStep step = content.Step("ab", zoneA, zoneB);

            Assert.That(step.Evaluate(zoneA, context).Kind, Is.EqualTo(StepEvaluationKind.Progressed));
            Assert.That(step.Evaluate(zoneB, context).Kind, Is.EqualTo(StepEvaluationKind.Completed));
        }

        [Test]
        public void Sequence_LaterActionFirst_IsViolation()
        {
            ActionDefinition zoneA = content.Action("ZoneA");
            ActionDefinition zoneB = content.Action("ZoneB");
            ActionStep step = content.Step("ab", zoneA, zoneB);

            StepEvaluation evaluation = step.Evaluate(zoneB, context);

            Assert.That(evaluation.Kind, Is.EqualTo(StepEvaluationKind.Violation));
            Assert.That(evaluation.Message, Does.Contain("ZoneA"));
        }

        [Test]
        public void Sequence_LaterActionFirst_IsIgnored_WhenOrderViolationDisabled()
        {
            ActionDefinition zoneA = content.Action("ZoneA");
            ActionDefinition zoneB = content.Action("ZoneB");
            ActionStep step = content.Step("ab", new[] { new RequiredAction(zoneA), new RequiredAction(zoneB) },
                wrongOrderIsViolation: false);

            Assert.That(step.Evaluate(zoneB, context).Kind, Is.EqualTo(StepEvaluationKind.Ignored));
        }

        [Test]
        public void StateRequirement_Unmet_IsViolation_Met_Completes()
        {
            ActionDefinition startPump = content.Action("StartPump");
            StateFlag valveOpen = content.Flag("ValveOpen");
            ActionStep step = content.Step("pump",
                new[] { new RequiredAction(startPump, new StateRequirement(valveOpen, true, "Клапан закрыт")) });

            StepEvaluation whenClosed = step.Evaluate(startPump, context);
            worldState.Set(valveOpen, true);
            StepEvaluation whenOpen = step.Evaluate(startPump, context);

            Assert.That(whenClosed.Kind, Is.EqualTo(StepEvaluationKind.Violation));
            Assert.That(whenClosed.Message, Is.EqualTo("Клапан закрыт"));
            Assert.That(whenOpen.Kind, Is.EqualTo(StepEvaluationKind.Completed));
        }

        [Test]
        public void ForbiddenAction_IsViolation_UnrelatedAction_IsIgnored()
        {
            ActionDefinition press = content.Action("Press");
            ActionDefinition emergency = content.Action("Emergency");
            ActionDefinition other = content.Action("Other");
            ActionStep step = content.Step("press", new[] { new RequiredAction(press) },
                new[] { new ForbiddenAction(emergency) });

            Assert.That(step.Evaluate(emergency, context).Kind, Is.EqualTo(StepEvaluationKind.Violation));
            Assert.That(step.Evaluate(other, context).Kind, Is.EqualTo(StepEvaluationKind.Ignored));
            Assert.That(step.Involves(press), Is.True);
            Assert.That(step.Involves(emergency), Is.False);
        }
    }
}
