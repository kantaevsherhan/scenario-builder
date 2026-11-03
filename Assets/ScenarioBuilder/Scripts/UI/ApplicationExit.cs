using UnityEngine;

namespace ScenarioBuilder.UI
{
    public static class ApplicationExit
    {
        /// <summary>A browser tab cannot be closed by the page, so the web build has no exit.</summary>
        public static bool IsSupported => Application.platform != RuntimePlatform.WebGLPlayer;

        /// <summary>Closes the player; in the editor leaves Play Mode instead.</summary>
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            if (IsSupported)
            {
                Application.Quit();
            }
#endif
        }
    }
}
