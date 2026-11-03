using UnityEngine;

namespace ScenarioBuilder.Logging
{
    /// <summary>Prints journal entries to the Unity console.</summary>
    [CreateAssetMenu(menuName = "Scenario Builder/Loggers/Console Logger", fileName = "ConsoleLogger", order = 60)]
    public sealed class ConsoleScenarioLogger : ScenarioLogger
    {
        [SerializeField, Tooltip("Выводить каждое полученное действие. Удобно для отладки, но шумно.")]
        private bool includeActions;

        public override void Write(JournalEntry entry)
        {
            if (!includeActions && entry.Type == JournalEventTypes.ActionReceived)
            {
                return;
            }

            Debug.Log(JournalFormatter.Format(entry));
        }
    }
}
