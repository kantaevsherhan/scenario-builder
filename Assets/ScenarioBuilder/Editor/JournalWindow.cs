using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Logging;
using ScenarioBuilder.UI;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// Opens saved run journals (or the current run in Play Mode) and re-analyzes them
    /// with any analyzer, without replaying the scenario.
    /// </summary>
    public sealed class JournalWindow : EditorWindow
    {
        private IReadOnlyList<string> files = new List<string>();
        private RunJournal journal;
        private string journalTitle;
        private ScenarioAnalyzer analyzer;
        private AnalysisReport report;
        private Vector2 filesScroll;
        private Vector2 contentScroll;

        [MenuItem("Tools/Scenario Builder/Журнал и анализ")]
        public static void Open() => GetWindow<JournalWindow>("Журнал сценария");

        private void OnEnable() => RefreshFiles();

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
                {
                    DrawSources();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawJournal();
                }
            }
        }

        private void DrawSources()
        {
            EditorGUILayout.LabelField("Источник журнала", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Текущее прохождение (Play Mode)"))
                {
                    LoadCurrentRun();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Обновить список"))
                {
                    RefreshFiles();
                }

                if (GUILayout.Button("Открыть папку"))
                {
                    Directory.CreateDirectory(JournalStorage.DirectoryPath);
                    EditorUtility.RevealInFinder(JournalStorage.DirectoryPath);
                }
            }

            EditorGUILayout.LabelField("Сохранённые журналы", EditorStyles.miniBoldLabel);
            filesScroll = EditorGUILayout.BeginScrollView(filesScroll);
            if (files.Count == 0)
            {
                EditorGUILayout.HelpBox("Файлов пока нет. Их создаёт JSON File Logger по окончании прохождения.", MessageType.Info);
            }

            foreach (string file in files)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(file), EditorStyles.miniButton))
                {
                    LoadFile(file);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawJournal()
        {
            if (journal == null)
            {
                EditorGUILayout.HelpBox("Выберите журнал слева.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(journalTitle, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Сценарий / режим", $"{journal.ScenarioName} / {journal.ModeName}");

            List<ScenarioAnalyzer> analyzers = ScenarioEditorGUI.FindAssets<ScenarioAnalyzer>();
            if (analyzer == null && analyzers.Count > 0)
            {
                analyzer = analyzers[0];
            }

            int current = analyzers.IndexOf(analyzer);
            int chosen = EditorGUILayout.Popup(new GUIContent("Анализатор", "Журнал оценивается заново при каждой смене анализатора."),
                current, analyzers.Select(item => new GUIContent(ScenarioEditorGUI.Label(item))).ToArray());
            if (chosen != current)
            {
                analyzer = analyzers[chosen];
                report = null;
            }

            if (report == null && analyzer != null)
            {
                report = analyzer.Analyze(journal);
            }

            contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
            if (report != null)
            {
                DrawReport(report);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Записи ({journal.Entries.Count})", EditorStyles.boldLabel);
            foreach (JournalEntry entry in journal.Entries)
            {
                EditorGUILayout.LabelField(JournalFormatter.Format(entry), EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawReport(AnalysisReport report)
        {
            EditorGUILayout.HelpBox($"{report.AnalyzerName}: {report.Score} из {report.MaxScore}", MessageType.None);
            foreach (string warning in report.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 380;
            foreach (StepReportLine line in report.Lines)
            {
                EditorGUILayout.LabelField(line.Title, $"{UiPalette.Label(line.Result)}, попыток: {line.Attempts}, баллы: {line.Points:+0;-0;0}");
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void RefreshFiles() => files = JournalStorage.ListFiles();

        private void LoadFile(string path)
        {
            SetJournal(JournalStorage.Load(path), Path.GetFileName(path));
        }

        private void LoadCurrentRun()
        {
            var runner = FindAnyObjectByType<ScenarioRunner>();
            if (runner == null || runner.Session == null)
            {
                ShowNotification(new GUIContent("В сцене нет активного прохождения"));
                return;
            }

            SetJournal(runner.Session.Journal, "Текущее прохождение");
        }

        private void SetJournal(RunJournal value, string title)
        {
            journal = value;
            journalTitle = title;
            report = null;
        }
    }
}
