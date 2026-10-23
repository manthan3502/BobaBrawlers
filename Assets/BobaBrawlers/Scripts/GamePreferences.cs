using UnityEngine;

namespace BobaBrawlers
{
    public static class GamePreferences
    {
        public static readonly string[] SkinNames = { "BOBA BRUISER", "MANGO MAYHEM", "MATCHA NINJA" };
        public static readonly Color[] SkinColors = { Palette.Blue, Palette.Orange, Palette.Lime };
        public static int Skin { get => Mathf.Clamp(PlayerPrefs.GetInt("Boba.Skin", 0), 0, 2); set { PlayerPrefs.SetInt("Boba.Skin", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); } }
        public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt("Boba.Unlocked", 1), 1, 3);
        public static bool Shake { get => PlayerPrefs.GetInt("Boba.Shake", 1) == 1; set { PlayerPrefs.SetInt("Boba.Shake", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static bool Fullscreen { get => PlayerPrefs.GetInt("Boba.Fullscreen", 0) == 1; set { PlayerPrefs.SetInt("Boba.Fullscreen", value ? 1 : 0); Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; PlayerPrefs.Save(); } }
        public static bool ReducedMotion { get => PlayerPrefs.GetInt("Boba.ReducedMotion", 0) == 1; set { PlayerPrefs.SetInt("Boba.ReducedMotion", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static int Best(int level) => PlayerPrefs.GetInt("Boba.Best." + level, 0);
        public static int BestCombo => PlayerPrefs.GetInt("Boba.BestCombo", 0);
        public static void Complete(int index, int points, int combo)
        {
            PlayerPrefs.SetInt("Boba.Unlocked", Mathf.Max(Unlocked, Mathf.Min(3, index + 2)));
            RecordPerformance(index, points, combo);
        }
        public static void RecordPerformance(int index, int points, int combo)
        {
            PlayerPrefs.SetInt("Boba.Best." + index, Mathf.Max(Best(index), points));
            PlayerPrefs.SetInt("Boba.BestCombo", Mathf.Max(BestCombo, combo)); PlayerPrefs.Save();
        }
    }
}
