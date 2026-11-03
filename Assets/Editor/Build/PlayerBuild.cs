using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScenarioBuilder.Build
{
    /// <summary>Shared part of the Windows and web builds: scene list, clean output folder, summary in the log.</summary>
    internal static class PlayerBuild
    {
        private const string DemoScenePath = "Assets/ScenarioBuilder/Demo/Scenes/DemoScenario.unity";

        public static BuildReport Run(string tag, BuildTarget target, BuildTargetGroup group, string locationPath, string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            var options = new BuildPlayerOptions
            {
                scenes = ScenesWithDemoFirst(),
                locationPathName = locationPath,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[{tag}] {summary.result}: {summary.outputPath}, " +
                      $"{summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalErrors} errors, " +
                      $"{summary.totalWarnings} warnings, {summary.totalTime:mm\\:ss}");
            return report;
        }

        public static bool Succeeded(BuildReport report) => report.summary.result == BuildResult.Succeeded;

        private static string[] ScenesWithDemoFirst()
        {
            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .Where(path => !string.Equals(path, DemoScenePath, StringComparison.Ordinal))
                .ToArray();
            return new[] { DemoScenePath }.Concat(enabledScenes).ToArray();
        }
    }
}
