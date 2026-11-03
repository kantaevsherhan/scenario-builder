using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScenarioBuilder.Build
{
    /// <summary>
    /// Builds the Windows (Standalone Win64) player into Builds/Windows.
    /// CLI: unity run . -- -executeMethod ScenarioBuilder.Build.WindowsBuild.BuildFromCommandLine
    /// </summary>
    public static class WindowsBuild
    {
        private const string DemoScenePath = "Assets/ScenarioBuilder/Demo/Scenes/DemoScenario.unity";
        private const string OutputPath = "Builds/Windows/ScenarioBuilder.exe";

        [MenuItem("Build/Windows")]
        public static void BuildFromMenu()
        {
            BuildReport report = Build();
            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(OutputPath);
            }
        }

        public static void BuildFromCommandLine()
        {
            BuildReport report = Build();
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildReport Build()
        {
            var options = new BuildPlayerOptions
            {
                scenes = ScenesWithDemoFirst(),
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            string outputDirectory = Path.GetDirectoryName(OutputPath);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[WindowsBuild] {summary.result}: {summary.outputPath}, " +
                      $"{summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalErrors} errors, " +
                      $"{summary.totalWarnings} warnings, {summary.totalTime:mm\\:ss}");
            return report;
        }

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
