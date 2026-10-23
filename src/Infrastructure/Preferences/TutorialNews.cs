using System.IO;

namespace AniimoAffinities
{
    internal static class TutorialNews
    {
        // Feature-specific marker: future tours use their own marker, not the app version.
        internal static readonly string FilePath = PreferenceFiles.FilePath("interactive-features-tour-v08.txt");
        internal static bool PrepareStartup()
        {
            bool existing = File.Exists(SessionState.FilePath) || File.Exists(TutorialProgress.FilePath) || File.Exists(TutorialPreferences.FilePath);
            if (File.Exists(FilePath))
                return false;
            PreferenceFiles.WriteText(FilePath, existing ? "returning" : "new");
            return existing;
        }
        internal static void PrepareProgress(TutorialProgress progress)
        {
            progress.Sections = TutorialProgress.News;
            progress.Step = 0;
            progress.Pending = false;
            progress.Completed &= ~TutorialProgress.News;
            progress.Started &= ~TutorialProgress.News;
            progress.SectionSteps[7] = 0;
            progress.ViewedSteps[7] = 0;
        }
        internal static bool ReturningUser
        {
            get
            {
                return File.Exists(FilePath) && File.ReadAllText(FilePath).Trim() == "returning";
            }
        }
    }
}
