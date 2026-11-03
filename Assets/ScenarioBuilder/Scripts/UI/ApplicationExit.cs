using UnityEngine;

namespace ScenarioBuilder.UI
{
    public static class ApplicationExit
    {
        /// <summary>Closes the player; in the editor leaves Play Mode instead.</summary>
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
