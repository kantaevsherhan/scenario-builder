using System;
using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>
    /// Step-by-step spotlight tour. On the first run of a mode with hints it walks through the interface,
    /// then points at what each newly opened step needs. Can be turned off in the settings.
    /// </summary>
    public sealed class GuidedTour : MonoBehaviour, IClosablePanel
    {
        [Serializable]
        private sealed class IntroStop
        {
            public GameObject target;
            public string title;
            [TextArea(2, 4)] public string text;
        }

        [SerializeField] private ScenarioRunner runner;
        [SerializeField] private SpotlightView view;

        [SerializeField, Tooltip("Вводная часть тура: что показать и что сказать.")]
        private List<IntroStop> intro = new();

        private readonly HashSet<StepState> hinted = new();
        private readonly List<StepState> hintQueue = new();
        private ScenarioSession session;
        private ScenarioActionSource[] sources = Array.Empty<ScenarioActionSource>();
        private int introIndex = -1;
        private int hintIndex = -1;
        private bool hintsPending;

        public bool IsOpen => view.IsVisible;

        private bool HintsAllowed => session != null && session.IsRunning && session.Mode.ShowHints && ScenarioPreferences.GuidedTour;

        private void Awake()
        {
            view.NextClicked += Next;
            view.SkipClicked += Hide;
            runner.SessionStarted += OnSessionStarted;
            ScenarioPreferences.Changed += OnPreferencesChanged;
        }

        private void OnDestroy()
        {
            runner.SessionStarted -= OnSessionStarted;
            ScenarioPreferences.Changed -= OnPreferencesChanged;
            SetSession(null);
        }

        private void Update()
        {
            if (hintsPending && introIndex < 0)
            {
                hintsPending = false;
                StartHints();
            }
        }

        public void StartIntro()
        {
            if (intro.Count == 0)
            {
                return;
            }

            hintIndex = -1;
            introIndex = 0;
            ShowIntroStop();
        }

        /// <summary>Closes the tour. Steps that are open now are not hinted again.</summary>
        public void Hide()
        {
            if (introIndex >= 0)
            {
                ScenarioPreferences.IntroSeen = true;
            }

            introIndex = -1;
            hintIndex = -1;
            if (session != null)
            {
                hinted.UnionWith(session.ActiveSteps);
            }

            view.Hide();
        }

        private void Next()
        {
            if (introIndex >= 0)
            {
                introIndex++;
                if (introIndex < intro.Count)
                {
                    ShowIntroStop();
                    return;
                }

                introIndex = -1;
                ScenarioPreferences.IntroSeen = true;
                view.Hide();
                StartHints();
                return;
            }

            ShowHint(hintIndex + 1);
        }

        private void OnSessionStarted(ScenarioSession newSession)
        {
            SetSession(newSession);
            sources = FindObjectsByType<ScenarioActionSource>();
            hinted.Clear();
            introIndex = -1;
            hintIndex = -1;
            view.Hide();

            if (session.Mode.ShowHints && ScenarioPreferences.GuidedTour && !ScenarioPreferences.IntroSeen)
            {
                StartIntro();
            }
            else
            {
                hintsPending = true;
            }
        }

        private void SetSession(ScenarioSession newSession)
        {
            if (session != null)
            {
                session.StepChanged -= OnStepChanged;
                session.Finished -= Hide;
            }

            session = newSession;
            if (session != null)
            {
                session.StepChanged += OnStepChanged;
                session.Finished += Hide;
            }
        }

        private void OnPreferencesChanged()
        {
            if (!ScenarioPreferences.GuidedTour && IsOpen)
            {
                Hide();
            }
        }

        private void OnStepChanged(StepState step)
        {
            if (step.Status == StepStatus.Active && !hinted.Contains(step))
            {
                hintsPending = true;
            }

            bool isShown = hintIndex >= 0 && hintIndex < hintQueue.Count && hintQueue[hintIndex] == step;
            if (isShown)
            {
                if (step.Status == StepStatus.Active)
                {
                    ShowHint(hintIndex);
                }
                else
                {
                    ShowHint(hintIndex + 1);
                }
            }
        }

        private void StartHints()
        {
            if (!HintsAllowed || IsOpen)
            {
                return;
            }

            hintQueue.Clear();
            hintQueue.AddRange(session.ActiveSteps.Where(step => !hinted.Contains(step)));
            ShowHint(0);
        }

        private void ShowHint(int index)
        {
            while (index < hintQueue.Count && hintQueue[index].Status != StepStatus.Active)
            {
                index++;
            }

            if (index >= hintQueue.Count || !HintsAllowed)
            {
                hintIndex = -1;
                view.Hide();
                return;
            }

            hintIndex = index;
            StepState step = hintQueue[index];
            hinted.Add(step);
            ScenarioActionSource source = SourceFor(step);
            int left = hintQueue.Skip(index + 1).Count(item => item.Status == StepStatus.Active);
            string title = hintQueue.Count > 1 ? $"Задание {index + 1} из {hintQueue.Count}" : "Задание";
            string text = $"<b>{step.Definition.Instruction}</b>\n{(source != null ? source.Hint : string.Empty)}\n" +
                          "<size=75%><color=#8A94A8>Подсказки выключаются в настройках.</color></size>";

            view.Show(new SpotlightTarget(source != null ? source.gameObject : null), title, text,
                string.Empty, left > 0 ? "Далее" : "Понятно");
        }

        private void ShowIntroStop()
        {
            IntroStop stop = intro[introIndex];
            bool isLast = introIndex == intro.Count - 1;
            view.Show(new SpotlightTarget(stop.target), stop.title, stop.text,
                $"{introIndex + 1} / {intro.Count}", isLast ? "Начать" : "Далее");
        }

        private ScenarioActionSource SourceFor(StepState step)
        {
            ActionDefinition action = step.ExpectedActions.FirstOrDefault();
            return action == null ? null : sources.FirstOrDefault(source => source != null && source.Actions.Contains(action));
        }
    }
}
