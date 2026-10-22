using System;
using System.IO;

namespace AniimoAffinities
{
    internal static class TutorialPreferences
    {
        internal static readonly string FilePath = PreferenceFiles.FilePath("tutorial.txt");
        internal static bool Hidden
        {
            get
            {
                return Load(FilePath);
            }
        }
        internal static bool Load(string path)
        {
            try
            {
                return File.ReadAllText(path).Trim() == "hide-v1";
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        internal static void Save(bool hidden, string path = null)
        {
            PreferenceFiles.WriteText(path ?? FilePath, hidden ? "hide-v1" : "show-v1");
        }
    }
}
