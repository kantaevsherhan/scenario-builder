using NUnit.Framework;
using ScenarioBuilder.Core;
using ScenarioBuilder.Steps;
using static ScenarioBuilder.Tests.TestContent;

namespace ScenarioBuilder.Tests
{
    public sealed class ScenarioValidatorTests
    {
        private TestContent content;

        [SetUp]
        public void SetUp() => content = new TestContent();

        [TearDown]
        public void TearDown() => content.Dispose();

        [Test]
        public void ValidScenario_HasNoIssues()
        {
            ActionStep a = content.Step("a", content.Action("x"));
            ScenarioDefinition scenario = content.Scenario(new StepDefinition[] { a }, content.Training(After(a)));

            Assert.That(ScenarioValidator.Validate(scenario), Is.Empty);
        }

        [Test]
        public void StepMissingFromMode_AndDuplicateStep_AreReported()
        {
            ActionStep a = content.Step("a", content.Action("x"));
            ActionStep b = content.Step("b", content.Action("y"));
            ScenarioDefinition scenario = content.Scenario(new StepDefinition[] { a, b }, content.Training(After(a), With(a)));

            var issues = ScenarioValidator.Validate(scenario);

            Assert.That(issues, Has.Some.Contains("«b» не включён"));
            Assert.That(issues, Has.Some.Contains("«a» указан несколько раз"));
        }

        [Test]
        public void StepWithoutActions_IsReported()
        {
            ActionStep empty = content.Step("empty");

            Assert.That(empty.Validate(), Is.Not.Empty);
        }
    }
}
