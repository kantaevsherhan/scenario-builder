using System;
using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Logging;
using ScenarioBuilder.Modes;
using UnityEngine;

namespace ScenarioBuilder.Core
{
    /// <summary>
    /// Scene entry point: owns the current session, forwards actions from event sources,
    /// feeds loggers and runs the analyzer when a run is over.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScenarioRunner : MonoBehaviour
    {
        [SerializeField, Tooltip("Сценарий, который запускается в этой сцене.")]
        private ScenarioDefinition scenario;

        [SerializeField, Tooltip("Режим при запуске сцены. Пусто — первый режим сценария.")]
        private ScenarioMode startMode;

        [SerializeField, Tooltip("Запускать сценарий автоматически при старте сцены.")]
        private bool startOnPlay = true;

        [SerializeField, Tooltip("Анализаторы, доступные для оценки и повторной оценки. " +
                                 "По окончании прохождения используется анализатор режима, а если он не задан — первый из списка.")]
        private List<ScenarioAnalyzer> analyzers = new();

        [SerializeField, Tooltip("Куда дополнительно пишется журнал: консоль, файл и т. д.")]
        private List<ScenarioLogger> loggers = new();

        /// <summary>Raised before the new session starts, so listeners can subscribe to its events.</summary>
        public event Action<ScenarioSession> SessionStarted;

        public event Action<AnalysisReport> ReportReady;

        /// <summary>An action came from the scene. The flag is true when the action caused a violation.</summary>
        public event Action<ActionDefinition, bool> ActionRaised;

        public ScenarioDefinition Scenario => scenario;
        public ScenarioWorldState WorldState { get; } = new();
        public ScenarioSession Session { get; private set; }
        public AnalysisReport LastReport { get; private set; }
        public IReadOnlyList<ScenarioAnalyzer> Analyzers => analyzers;

        private int violationCount;

        private ScenarioMode DefaultMode =>
            startMode != null ? startMode : scenario != null ? scenario.Modes.FirstOrDefault() : null;

        private void Start()
        {
            if (startOnPlay)
            {
                StartRun(DefaultMode);
            }
        }

        private void OnDestroy() => StopSession();

        public void StartRun(ScenarioMode mode)
        {
            if (scenario == null || mode == null)
            {
                Debug.LogError("ScenarioRunner: не задан сценарий или режим.", this);
                return;
            }

            StopSession();
            LastReport = null;
            Session = new ScenarioSession(scenario.Title, mode, WorldState, () => Time.time);
            Session.EntryWritten += WriteToLoggers;
            Session.Finished += OnSessionFinished;
            Session.ViolationRaised += CountViolation;

            ForEachLogger(logger => logger.BeginRun(Session.Journal));
            SessionStarted?.Invoke(Session);
            Session.Start();
        }

        public void RestartRun() => StartRun(Session != null ? Session.Mode : DefaultMode);

        public void RaiseAction(ActionDefinition action)
        {
            if (action == null)
            {
                return;
            }

            int violationsBefore = violationCount;
            Session?.HandleAction(action);
            ActionRaised?.Invoke(action, violationCount > violationsBefore);
        }

        public void SetState(StateFlag flag, bool value) => WorldState.Set(flag, value);

        /// <summary>Scores the journal of the current or last run. Can be called again with another analyzer.</summary>
        public AnalysisReport Analyze(ScenarioAnalyzer analyzer)
        {
            if (Session == null || analyzer == null)
            {
                return null;
            }

            LastReport = analyzer.Analyze(Session.Journal);
            ReportReady?.Invoke(LastReport);
            return LastReport;
        }

        public ScenarioAnalyzer DefaultAnalyzerFor(ScenarioMode mode) =>
            mode != null && mode.DefaultAnalyzer != null ? mode.DefaultAnalyzer : analyzers.FirstOrDefault();

        private void OnSessionFinished()
        {
            ForEachLogger(logger => logger.EndRun(Session.Journal));
            Analyze(DefaultAnalyzerFor(Session.Mode));
        }

        private void StopSession()
        {
            if (Session == null)
            {
                return;
            }

            if (Session.IsRunning)
            {
                Session.Abort();
                ForEachLogger(logger => logger.EndRun(Session.Journal));
            }

            Session.EntryWritten -= WriteToLoggers;
            Session.Finished -= OnSessionFinished;
            Session.ViolationRaised -= CountViolation;
        }

        private void CountViolation(StepState step, string message) => violationCount++;

        private void WriteToLoggers(JournalEntry entry) => ForEachLogger(logger => logger.Write(entry));

        private void ForEachLogger(Action<ScenarioLogger> action)
        {
            foreach (ScenarioLogger logger in loggers.Where(logger => logger != null))
            {
                action(logger);
            }
        }
    }
}
