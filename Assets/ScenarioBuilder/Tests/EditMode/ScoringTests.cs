using System;
using System.Linq;
using NUnit.Framework;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Logging;
using ScenarioBuilder.Steps;
using UnityEngine;
using static ScenarioBuilder.Tests.TestContent;

namespace ScenarioBuilder.Tests
{
    /// <summary>Control examples from the specification plus re-analysis of stored journals.</summary>
    public sealed class ScoringTests
    {
        private TestContent content;
        private ActionDefinition wrong;

        [SetUp]
        public void SetUp()
        {
            content = new TestContent();
            wrong = content.Action("wrong");
        }

        [TearDown]
        public void TearDown() => content.Dispose();

        [Test]
        public void Training_FirstSecondThirdAttempt_Gives10Plus5Plus0()
        {
            ActionDefinition[] actions = CreateActions(3);
            ActionStep[] steps = CreateGuardedSteps(actions);
            ScenarioSession session = Start(content.Training(steps.Select(After).ToArray()));

            session.HandleAction(actions[0]);
            session.HandleAction(wrong);
            session.HandleAction(actions[1]);
            session.HandleAction(wrong);
            session.HandleAction(wrong);
            session.HandleAction(actions[2]);

            AnalysisReport report = content.AttemptAnalyzer().Analyze(session.Journal);

            Assert.That(session.IsFinished, Is.True);
            Assert.That(report.Lines.Select(line => line.Points), Is.EqualTo(new[] { 10, 5, 0 }));
            Assert.That(report.Score, Is.EqualTo(15));
            Assert.That(report.MaxScore, Is.EqualTo(30));
            Assert.That(report.Warnings, Is.Empty);
        }

        [Test]
        public void Exam_TwoFailedTwoSkippedInGroupOfFour_Gives70()
        {
            ActionDefinition[] actions = CreateActions(5);
            ActionStep[] steps = CreateGuardedSteps(actions);
            var group1 = new StepGroup("G1", new[] { After(steps[0]), With(steps[1]), With(steps[2]), With(steps[3]) });
            var group2 = new StepGroup("G2", new[] { After(steps[4]) });
            ScenarioSession session = Start(content.Exam(2, group1, group2));

            session.HandleAction(wrong);
            session.HandleAction(actions[4]);

            AnalysisReport report = content.PenaltyAnalyzer().Analyze(session.Journal);

            Assert.That(session.IsFinished, Is.True);
            Assert.That(CountResults(report, StepResult.Failed), Is.EqualTo(2));
            Assert.That(CountResults(report, StepResult.Skipped), Is.EqualTo(2));
            Assert.That(report.Lines.Where(line => line.StepId != "s5").Sum(line => line.Points), Is.EqualTo(-30));
            Assert.That(report.Score, Is.EqualTo(70));
            Assert.That(report.Warnings, Is.Empty);
        }

        [Test]
        public void StoredJournal_CanBeReanalyzedWithAnotherAnalyzer()
        {
            ActionDefinition[] actions = CreateActions(2);
            ActionStep[] steps = CreateGuardedSteps(actions);
            ScenarioSession session = Start(content.Exam(2, new StepGroup("G1", steps.Select(After))));
            session.HandleAction(wrong);
            session.HandleAction(actions[1]);

            RunJournal restored = JsonUtility.FromJson<RunJournal>(JsonUtility.ToJson(session.Journal));
            AnalysisReport penalty = content.PenaltyAnalyzer().Analyze(restored);
            AnalysisReport attempts = content.AttemptAnalyzer().Analyze(restored);

            Assert.That(restored.Entries.Count, Is.EqualTo(session.Journal.Entries.Count));
            Assert.That(penalty.Score, Is.EqualTo(90));
            Assert.That(attempts.Score, Is.EqualTo(10));
            Assert.That(attempts.Warnings, Is.Not.Empty, "Exam journal analyzed by the training analyzer should be flagged.");
        }

        [Test]
        public void JournalWithoutOptionalData_IsAnalyzedWithWarnings()
        {
            var journal = new RunJournal("Manual", "Custom", DateTime.UtcNow);
            journal.Add(new JournalEntry(0f, JournalEventTypes.StepViolation, "s1", result: JournalResults.Violation));
            journal.Add(new JournalEntry(1f, JournalEventTypes.StepCompleted, "s1", result: JournalResults.Completed));
            journal.Add(new JournalEntry(2f, "SomeFutureEntryType", "s1"));

            AnalysisReport report = content.AttemptAnalyzer().Analyze(journal);

            Assert.That(report.Score, Is.EqualTo(5), "Attempts are derived from violations when the attempt field is missing.");
            Assert.That(report.Warnings.Count, Is.EqualTo(3), "Unfinished run, no step plan, no attempt field.");
        }

        [Test]
        public void EmptyJournal_GivesZeroWithWarning()
        {
            AnalysisReport report = content.PenaltyAnalyzer().Analyze(new RunJournal("Empty", "None", DateTime.UtcNow));

            Assert.That(report.Score, Is.Zero);
            Assert.That(report.Warnings, Is.Not.Empty);
        }

        private ActionDefinition[] CreateActions(int count) =>
            Enumerable.Range(1, count).Select(i => content.Action($"a{i}")).ToArray();

        /// <summary>Steps completed by their own action; the shared "wrong" action is a violation for each of them.</summary>
        private ActionStep[] CreateGuardedSteps(ActionDefinition[] actions) =>
            actions.Select((action, i) => content.Step($"s{i + 1}", new[] { new RequiredAction(action) },
                new[] { new ForbiddenAction(wrong) })).ToArray();

        private static int CountResults(AnalysisReport report, StepResult result) =>
            report.Lines.Count(line => line.Result == result);
    }
}
