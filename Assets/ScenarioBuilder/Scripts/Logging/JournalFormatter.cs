using System.Text;

namespace ScenarioBuilder.Logging
{
    /// <summary>Single-line text representation of an entry, shared by the console logger and the journal views.</summary>
    public static class JournalFormatter
    {
        public static string Format(JournalEntry entry)
        {
            var line = new StringBuilder();
            line.Append($"[{entry.Time,7:0.00}s] {entry.Type}");
            if (entry.HasStep)
            {
                line.Append($" | {entry.StepId}");
            }

            if (!string.IsNullOrEmpty(entry.Result))
            {
                line.Append($" | {entry.Result}");
            }

            if (!string.IsNullOrEmpty(entry.Message))
            {
                line.Append($" | {entry.Message}");
            }

            foreach (JournalField field in entry.Fields)
            {
                line.Append($" | {field.Key}={field.Value}");
            }

            return line.ToString();
        }
    }
}
