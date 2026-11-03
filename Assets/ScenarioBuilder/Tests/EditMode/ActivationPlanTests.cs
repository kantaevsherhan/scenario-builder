using System.Linq;
using NUnit.Framework;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Steps;
using static ScenarioBuilder.Tests.TestContent;

namespace ScenarioBuilder.Tests
{
    public sealed class ActivationPlanTests
    {
        private TestContent content;

        [SetUp]
        public void SetUp() => content = new TestContent();

        [TearDown]
        public void TearDown() => content.Dispose();

        [Test]
        public void MixedActivationFromSpec_BuildsExpectedStages()
        {
            StepDefinition[] s = Enumerable.Range(1, 12).Select(i => (StepDefinition)content.Step($"s{i}")).ToArray();

            var stages = ActivationPlan.BuildStages(new[]
            {
                After(s[0]), With(s[1]), With(s[2]), With(s[3]),
                After(s[4]), After(s[5]), After(s[6]), After(s[7]),
                After(s[8]), With(s[9]),
                After(s[10]), With(s[11]),
            });

            Assert.That(stages.Select(stage => stage.Count), Is.EqualTo(new[] { 4, 1, 1, 1, 1, 2, 2 }));
            Assert.That(stages[6], Is.EqualTo(new[] { s[10], s[11] }));
        }

        [Test]
        public void FirstEntryMarkedWithPrevious_StartsFirstStage()
        {
            StepDefinition a = content.Step("a");
            StepDefinition b = content.Step("b");

            var stages = ActivationPlan.BuildStages(new[] { With(a), With(b) });

            Assert.That(stages.Count, Is.EqualTo(1));
            Assert.That(stages[0], Is.EqualTo(new[] { a, b }));
        }

        [Test]
        public void EmptyEntries_AreSkipped()
        {
            StepDefinition a = content.Step("a");

            var stages = ActivationPlan.BuildStages(new[] { After(null), null, After(a) });

            Assert.That(stages.Count, Is.EqualTo(1));
        }
    }
}
