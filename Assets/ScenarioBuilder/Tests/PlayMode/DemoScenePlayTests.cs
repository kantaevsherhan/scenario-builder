using System.Collections;
using System.Linq;
using NUnit.Framework;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScenarioBuilder.Tests
{
    /// <summary>
    /// Plays the demo scene end to end through its real inputs: UI buttons, the pressure slider,
    /// trigger zones and collisions with the player.
    /// </summary>
    public sealed class DemoScenePlayTests
    {
        private const string SceneName = "DemoScenario";

        private ScenarioRunner runner;
        private Rigidbody player;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            runner = Object.FindAnyObjectByType<ScenarioRunner>();
            player = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        }

        [UnityTest]
        public IEnumerator TrainingRun_AllStepsFirstTry_GivesMaximum()
        {
            runner.StartRun(Mode("Тренировка"));

            Click("PowerButton");
            Click("LightsButton");
            Click("OpenValveButton");
            SetPressure(100);
            yield return Touch("RedPost");
            yield return Enter("ZoneA");
            yield return Enter("ZoneB");
            Click("StartPumpButton");
            yield return Enter("ExitZone");
            Click("SensorsButton");
            yield return Touch("BluePost");
            SetPressure(0);
            Click("FinishButton");

            Assert.That(runner.Session.IsFinished, Is.True, ActiveSteps());
            Assert.That(runner.LastReport.Score, Is.EqualTo(120));
            Assert.That(runner.LastReport.MaxScore, Is.EqualTo(120));
        }

        [UnityTest]
        public IEnumerator ExamRun_TwoViolationsInFirstGroup_Gives70()
        {
            runner.StartRun(Mode("Экзамен"));

            Click("EmergencyButton");
            Click("StartPumpButton");
            Assert.That(runner.Session.CurrentGroupNumber, Is.EqualTo(2), "First group should be closed by the threshold.");

            Click("OpenValveButton");
            yield return Touch("RedPost");
            yield return Enter("ZoneA");
            yield return Enter("ZoneB");
            Click("StartPumpButton");
            yield return Enter("ExitZone");
            Click("SensorsButton");
            yield return Touch("BluePost");
            SetPressure(100);
            SetPressure(0);
            Click("FinishButton");

            AnalysisReport report = runner.LastReport;
            Assert.That(runner.Session.IsFinished, Is.True, ActiveSteps());
            Assert.That(report.Lines.Count(line => line.Result == StepResult.Failed), Is.EqualTo(2));
            Assert.That(report.Lines.Count(line => line.Result == StepResult.Skipped), Is.EqualTo(2));
            Assert.That(report.Score, Is.EqualTo(70));

            AnalysisReport reanalyzed = runner.Analyze(runner.Analyzers.First(analyzer => analyzer is AttemptScoreAnalyzer));
            Assert.That(reanalyzed.Score, Is.EqualTo(80), "8 completed steps × 10 when scored by attempts.");
        }

        [UnityTest]
        public IEnumerator Training_WrongOrderInZones_IsViolationAndRetry()
        {
            runner.StartRun(Mode("Тренировка"));
            Click("PowerButton");
            Click("LightsButton");
            Click("OpenValveButton");
            SetPressure(100);
            yield return Touch("RedPost");

            yield return Enter("ZoneB");

            StepState zones = runner.Session.Steps.First(step => step.Id == "zones-a-b");
            Assert.That(zones.Status, Is.EqualTo(StepStatus.Active));
            Assert.That(zones.Attempt, Is.EqualTo(2));
        }

        private Modes.ScenarioMode Mode(string displayName) =>
            runner.Scenario.Modes.First(mode => mode.DisplayName == displayName);

        private static void Click(string buttonName) =>
            GameObject.Find(buttonName).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

        private static void SetPressure(float value) =>
            GameObject.Find("PressureSlider").GetComponent<UnityEngine.UI.Slider>().value = value;

        private IEnumerator Enter(string zoneName)
        {
            Vector3 zone = GameObject.Find(zoneName).transform.position;
            yield return MovePlayer(new Vector3(zone.x, 1f, zone.z));
        }

        private IEnumerator Touch(string postName)
        {
            Vector3 post = GameObject.Find(postName).transform.position;
            yield return MovePlayer(new Vector3(post.x, 1f, post.z - 0.6f));
        }

        private IEnumerator MovePlayer(Vector3 position)
        {
            player.position = position;
            player.transform.position = position;
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private string ActiveSteps() =>
            "Active: " + string.Join(", ", runner.Session.ActiveSteps.Select(step => step.Id));
    }
}
