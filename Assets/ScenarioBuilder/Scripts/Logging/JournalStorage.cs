using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder.Logging
{
    /// <summary>Reads and writes run journals as JSON files.</summary>
    public static class JournalStorage
    {
        public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Journals");

        public static string Save(RunJournal journal)
        {
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, $"run_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
            File.WriteAllText(path, JsonUtility.ToJson(journal, true));
            return path;
        }

        public static RunJournal Load(string path) => JsonUtility.FromJson<RunJournal>(File.ReadAllText(path));

        /// <summary>Saved journal files, newest first.</summary>
        public static IReadOnlyList<string> ListFiles()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(DirectoryPath, "*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();
        }
    }
}
