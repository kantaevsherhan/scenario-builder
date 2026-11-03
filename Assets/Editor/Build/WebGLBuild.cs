using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ScenarioBuilder.Build
{
    /// <summary>
    /// Builds the web player into Builds/WebGL, ready for GitHub Pages: index.html at the root, no Unity compression
    /// (Pages gzips the files itself and cannot send headers for .br/.gz), the responsive page template,
    /// settings that keep the download small (High stripping, size-optimized IL2CPP, Disk Size LTO).
    /// CLI: unity run . -- -buildTarget WebGL -executeMethod ScenarioBuilder.Build.WebGLBuild.BuildFromCommandLine
    /// </summary>
    public static class WebGLBuild
    {
        private const string OutputPath = "Builds/WebGL";

        // The page shows "N of M MB" while loading; the file sizes are written in after the build.
        private const string FileSizesPlaceholder = "/*__BUILD_FILES__*/{}";

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
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            EditorUserBuildSettings.SetPlatformSettings("WebGL", "CodeOptimization", "DiskSizeLTO");
            AssetDatabase.SaveAssets();

            BuildReport report = PlayerBuild.Run("WebGLBuild", BuildTarget.WebGL, BuildTargetGroup.WebGL, OutputPath, OutputPath);
            if (PlayerBuild.Succeeded(report))
            {
                WriteFileSizes();
            }

            return report;
        }

        private static void WriteFileSizes()
        {
            IEnumerable<string> sizes = Directory.GetFiles(Path.Combine(OutputPath, "Build"))
                .Where(file => !file.EndsWith(".loader.js"))
                .Select(file => $"\"{Path.GetFileName(file)}\": {new FileInfo(file).Length}");
            string page = Path.Combine(OutputPath, "index.html");
            File.WriteAllText(page, File.ReadAllText(page).Replace(FileSizesPlaceholder, "{" + string.Join(", ", sizes) + "}"));
        }
    }
}
