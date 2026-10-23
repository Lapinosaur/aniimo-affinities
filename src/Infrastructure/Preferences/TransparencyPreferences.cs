using System;
using System.IO;
namespace AniimoAffinities
{
    internal static class TransparencyPreferences
    {
        internal static bool Locked = true, Children, Main;
        internal static event Action Changed;
        static TransparencyPreferences()
        {
            Load();
        }
        internal static void Load(string path = null)
        {
            Locked = true;
            Children = Main = false;
            try
            {
                var lines = File.ReadAllLines(path ?? PreferenceFiles.FilePath("transparency.txt"));
                bool value;
                if (lines.Length > 0 && bool.TryParse(lines[0], out value))
                    Locked = value;
                if (lines.Length > 1 && bool.TryParse(lines[1], out value))
                    Children = value;
                if (lines.Length > 2 && bool.TryParse(lines[2], out value))
                    Main = value;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        internal static void Save(bool locked, bool children, bool main, string path = null)
        {
            PreferenceFiles.WriteLines(path ?? PreferenceFiles.FilePath("transparency.txt"), new[] { locked.ToString(), children.ToString(), main.ToString() });
            Locked = locked;
            Children = children;
            Main = main;
            if (Changed != null)
                Changed();
        }
    }
}
