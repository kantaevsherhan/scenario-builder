using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ScenarioBuilder.Build
{
    /// <summary>
    /// Builds the web player into Builds/WebGL, ready for GitHub Pages: index.html at the root, no compression
    /// (Pages cannot send Content-Encoding headers for .br/.gz files), the responsive page template.
    /// CLI: unity run . -- -buildTarget WebGL -executeMethod ScenarioBuilder.Build.WebGLBuild.BuildFromCommandLine
    /// </summary>
    public static class WebGLBuild
    {
        private const string OutputPath = "Builds/WebGL";

        [MenuItem("Build/WebGL")]
        public static void BuildFromMenu()
        {
            if (PlayerBuild.Succeeded(Build()))
            {
                EditorUtility.RevealInFinder(OutputPath + "/index.html");
            }
        }

        public static void BuildFromCommandLine()
        {
            EditorApplication.Exit(PlayerBuild.Succeeded(Build()) ? 0 : 1);
        }

        private static BuildReport Build()
        {
            PlayerSettings.WebGL.template = "PROJECT:Responsive";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.dataCaching = true;
            AssetDatabase.SaveAssets();
            return PlayerBuild.Run("WebGLBuild", BuildTarget.WebGL, BuildTargetGroup.WebGL, OutputPath, OutputPath);
        }
    }
}
