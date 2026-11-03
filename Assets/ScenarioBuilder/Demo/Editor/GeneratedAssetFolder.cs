using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Demo.Editor
{
    /// <summary>
    /// A folder of generated assets that is rewritten in place: existing files keep their GUIDs,
    /// so a rebuild does not break references and keeps version-control diffs small.
    /// </summary>
    internal sealed class GeneratedAssetFolder
    {
        private readonly string root;
        private readonly List<Object> written = new();
        private readonly HashSet<string> writtenPaths = new();

        public GeneratedAssetFolder(string root)
        {
            this.root = root;
            EnsureFolder(root);
        }

        public T Write<T>(string subfolder, string assetName, T fresh, string extension = "asset") where T : Object
        {
            string folder = string.IsNullOrEmpty(subfolder) ? root : $"{root}/{subfolder}";
            EnsureFolder(folder);
            string path = $"{folder}/{assetName}.{extension}";
            writtenPaths.Add(path);
            fresh.name = assetName;

            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                written.Add(fresh);
                return fresh;
            }

            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            written.Add(existing);
            return existing;
        }

        /// <summary>Saves everything written in this pass and deletes files the generator no longer produces.</summary>
        public void Finish()
        {
            foreach (Object asset in written)
            {
                EditorUtility.SetDirty(asset);
            }

            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(path) && !writtenPaths.Contains(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
