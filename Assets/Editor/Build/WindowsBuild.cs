using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ScenarioBuilder.Build
{
    /// <summary>
    /// Builds the Windows (Standalone Win64) player into Builds/Windows.
    /// CLI: unity run . -- -executeMethod ScenarioBuilder.Build.WindowsBuild.BuildFromCommandLine
    /// </summary>
    public static class WindowsBuild
    {
        private const string OutputPath = "Builds/Windows/ScenarioBuilder.exe";

        [MenuItem("Build/Windows")]
        public static void BuildFromMenu()
        {
            if (PlayerBuild.Succeeded(Build()))
            {
                EditorUtility.RevealInFinder(OutputPath);
            }
        }

        public static void BuildFromCommandLine()
        {
            EditorApplication.Exit(PlayerBuild.Succeeded(Build()) ? 0 : 1);
        }

        private static BuildReport Build() =>
            PlayerBuild.Run("WindowsBuild", BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone,
                OutputPath, "Builds/Windows");
    }
}
