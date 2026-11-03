using UnityEngine;

namespace ScenarioBuilder.Logging
{
    /// <summary>Saves the whole run journal as a JSON file when the run ends, so it can be re-analyzed later.</summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Loggers/JSON File Logger", fileName = "JsonFileLogger", order = 61)]
    public sealed class JsonFileScenarioLogger : ScenarioLogger
    {
        public string LastSavedPath { get; private set; }

        public override void Write(JournalEntry entry)
        {
        }

        public override void EndRun(RunJournal journal)
        {
            LastSavedPath = JournalStorage.Save(journal);
            Debug.Log($"Журнал прохождения сохранён: {LastSavedPath}");
        }
    }
}
