using System.IO;

namespace AniimoAffinities
{
    internal static class OnboardingPreferences
    {
        internal static bool ShouldShow(string root = null)
        {
            root = root ?? PreferenceFiles.Root;
            string marker = Path.Combine(root, "onboarding.txt");
            if (File.Exists(marker))
                return File.ReadAllText(marker).Trim() != "complete-v1\nen";
            // Existing installations keep their preferences and their news/tutorial flow.
            foreach (string name in new[] { "session.txt", "tutorial-progress.txt", "tutorial.txt", "interactive-features-tour-v08.txt", "display.txt", "appearance.txt", "keys.txt", "teams.txt", "transparency.txt", "content-windows.txt" })
                if (File.Exists(Path.Combine(root, name)))
                    return false;
            return true;
        }
        internal static void Begin()
        {
            PreferenceFiles.WriteText(PreferenceFiles.FilePath("onboarding.txt"), "pending-v1");
        }
        internal static void Complete()
        {
            PreferenceFiles.WriteText(PreferenceFiles.FilePath("onboarding.txt"), "complete-v1\nen");
        }
    }
}
