using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder.Logging
{
    /// <summary>Complete record of one run. Analyzers work only with this data, never with the live session.</summary>
    [Serializable]
    public sealed class RunJournal
    {
        public const string CurrentFormatVersion = "1";

        [SerializeField] private string formatVersion = CurrentFormatVersion;
        [SerializeField] private string scenarioName;
        [SerializeField] private string modeName;
        [SerializeField] private string startedAtUtc;
        [SerializeField] private List<JournalEntry> entries = new();

        public RunJournal(string scenarioName, string modeName, DateTime startedAtUtc)
        {
            this.scenarioName = scenarioName;
            this.modeName = modeName;
            this.startedAtUtc = startedAtUtc.ToString("o");
        }

        public string FormatVersion => formatVersion;
        public string ScenarioName => scenarioName;
        public string ModeName => modeName;
        public string StartedAtUtc => startedAtUtc;
        public IReadOnlyList<JournalEntry> Entries => entries;

        public void Add(JournalEntry entry) => entries.Add(entry);
    }
}
