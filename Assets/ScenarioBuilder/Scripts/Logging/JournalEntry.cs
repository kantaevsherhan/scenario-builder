using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder.Logging
{
    /// <summary>Optional key/value data attached to an entry. Readers ignore keys they do not know.</summary>
    [Serializable]
    public struct JournalField
    {
        [SerializeField] private string key;
        [SerializeField] private string value;

        public JournalField(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public string Key => key;
        public string Value => value;
    }

    /// <summary>
    /// One journal record. Required contract: time, type, step id and result.
    /// Anything else goes into <see cref="Fields"/>, so new data never breaks existing readers.
    /// </summary>
    [Serializable]
    public sealed class JournalEntry
    {
        [SerializeField] private float time;
        [SerializeField] private string type;
        [SerializeField] private string stepId;
        [SerializeField] private string groupName;
        [SerializeField] private string result;
        [SerializeField] private string message;
        [SerializeField] private List<JournalField> fields = new();

        public JournalEntry(float time, string type, string stepId = "", string groupName = "",
            string result = "", string message = "")
        {
            this.time = time;
            this.type = type;
            this.stepId = stepId ?? string.Empty;
            this.groupName = groupName ?? string.Empty;
            this.result = result ?? string.Empty;
            this.message = message ?? string.Empty;
        }

        /// <summary>Seconds since the start of the run.</summary>
        public float Time => time;

        public string Type => type;
        public string StepId => stepId;
        public string GroupName => groupName;
        public string Result => result;
        public string Message => message;
        public IReadOnlyList<JournalField> Fields => fields;

        public bool HasStep => !string.IsNullOrEmpty(stepId);

        public JournalEntry With(string key, object value)
        {
            fields.Add(new JournalField(key, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));
            return this;
        }

        public bool TryGetField(string key, out string value)
        {
            foreach (JournalField field in fields)
            {
                if (field.Key == key)
                {
                    value = field.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            return TryGetField(key, out string text) && int.TryParse(text, out value);
        }
    }
}
