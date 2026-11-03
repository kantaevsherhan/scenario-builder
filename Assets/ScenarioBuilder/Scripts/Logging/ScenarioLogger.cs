using UnityEngine;

namespace ScenarioBuilder.Logging
{
    /// <summary>
    /// Output for journal entries (console, file, server...). The run journal itself is always kept
    /// by the session, loggers only decide where entries additionally go.
    /// Derive from this class to add a new logger.
    /// </summary>
    public abstract class ScenarioLogger : ScriptableObject
    {
        public virtual void BeginRun(RunJournal journal)
        {
        }

        public abstract void Write(JournalEntry entry);

        public virtual void EndRun(RunJournal journal)
        {
        }
    }
}
