using System;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Player-side settings that survive restarts (stored in PlayerPrefs).</summary>
    public static class ScenarioPreferences
    {
        private const string HighlightTargetsKey = "ScenarioBuilder.HighlightTargets";
        private const string GuidedTourKey = "ScenarioBuilder.GuidedTour";
        private const string IntroSeenKey = "ScenarioBuilder.IntroSeen";

        public static event Action Changed;

        /// <summary>Glow on the objects of the current step (only in modes with hints).</summary>
        public static bool HighlightTargets
        {
            get => GetBool(HighlightTargetsKey, true);
            set => SetBool(HighlightTargetsKey, value);
        }

        /// <summary>Spotlight tour: introduction and hints for active steps (only in modes with hints).</summary>
        public static bool GuidedTour
        {
            get => GetBool(GuidedTourKey, true);
            set => SetBool(GuidedTourKey, value);
        }

        public static bool IntroSeen
        {
            get => GetBool(IntroSeenKey, false);
            set => SetBool(IntroSeenKey, value);
        }

        private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
