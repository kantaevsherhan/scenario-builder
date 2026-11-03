using System.Collections;
using System.Linq;
using NUnit.Framework;
using ScenarioBuilder.Core;
using ScenarioBuilder.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScenarioBuilder.Tests
{
    /// <summary>Clicks the demo UI with a virtual mouse, the way a player does, instead of invoking onClick.</summary>
    public sealed class PointerClickTests
    {
        private const int IntroStops = 5;

        private Mouse mouse;
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
        private InputSettings.BackgroundBehavior background;
        private ScenarioRunner runner;
        private GuidedTour tour;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse = InputSystem.AddDevice<Mouse>();
            PlayerPrefs.DeleteAll();
            yield return SceneManager.LoadSceneAsync("DemoScenario", LoadSceneMode.Single);
            yield return null;
            runner = Object.FindAnyObjectByType<ScenarioRunner>();
            tour = Object.FindAnyObjectByType<GuidedTour>();
            Assert.That(runner.Session, Is.Null, "Nothing should run before Play.");
            yield return Click("PlayButton");
            Assert.That(runner.Session, Is.Not.Null);
            Assert.That(runner.Session.Mode.DisplayName, Is.EqualTo("Тренировка"));
        }

        [TearDown]
        public void RemoveMouse()
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            InputSystem.settings.backgroundBehavior = background;
            PlayerPrefs.DeleteAll();
        }

        [UnityTest]
        public IEnumerator FirstClickAfterSkippingTheTour_Counts()
        {
            Assert.That(tour.IsOpen, Is.True, "The tour should open on the first training run.");

            yield return Click("SkipButton");
            Assert.That(tour.IsOpen, Is.False);

            yield return Click("PowerButton");
            Assert.That(Step("power").Status, Is.EqualTo(StepStatus.Completed));
        }

        [UnityTest]
        public IEnumerator QuickNextClicks_WalkTheWholeTour_ThenTheFirstClickCounts()
        {
            int hints = runner.Session.ActiveSteps.Count();
            int clicks = 0;
            while (tour.IsOpen && clicks < 20)
            {
                yield return Click("NextButton");
                clicks++;
            }

            Assert.That(clicks, Is.EqualTo(IntroStops + hints), "Every Next click should land on Next, not on a button that slid under the cursor.");
            yield return Click("PowerButton");
            Assert.That(Step("power").Status, Is.EqualTo(StepStatus.Completed));
        }

        [UnityTest]
        public IEnumerator HighlightedTarget_IsClickableAsSoonAsTheHintAppears()
        {
            for (int i = 0; i < IntroStops; i++)
            {
                yield return Click("NextButton");
            }

            Assert.That(tour.IsOpen, Is.True, "The first step hint should follow the intro.");
            yield return Click("PowerButton");
            Assert.That(Step("power").Status, Is.EqualTo(StepStatus.Completed));
            yield return Click("LightsButton");
            Assert.That(Step("lights").Status, Is.EqualTo(StepStatus.Completed));
        }

        private StepState Step(string id) => runner.Session.Steps.First(step => step.Id == id);

        private IEnumerator Click(string objectName)
        {
            var rect = (RectTransform)GameObject.Find(objectName).transform;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) / 2f);

            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
        }
    }
}
