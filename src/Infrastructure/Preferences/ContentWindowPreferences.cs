using System;
using System.IO;
namespace AniimoAffinities
{
    internal static class ContentWindowPreferences
    {
        internal static event Action Changed;
        internal static bool KeepVisible = Load();
        private static bool Load()
        {
            try
            {
                bool value;
                return !bool.TryParse(File.ReadAllText(PreferenceFiles.FilePath("content-windows.txt")), out value) || value;
            }
            catch (IOException) { return true; }
            catch (UnauthorizedAccessException) { return true; }
        }
        internal static void Save(bool value)
        {
            PreferenceFiles.WriteText(PreferenceFiles.FilePath("content-windows.txt"), value.ToString());
            KeepVisible = value;
            if (Changed != null)
                Changed();
        }
    }
}
