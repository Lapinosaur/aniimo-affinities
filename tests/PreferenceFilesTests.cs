using System;
using System.IO;
using System.Linq;
namespace AniimoAffinities
{
    internal static class PreferenceFilesTests
    {
        internal static void Run()
        {
            string folder = Path.Combine(PreferenceFiles.Root, "atomic-tests");
            string file = Path.Combine(folder, "preferences.txt");
            PreferenceFiles.WriteText(file, "original");
            PreferenceFiles.WriteText(file, "replaced → é");
            if (File.ReadAllText(file) != "replaced → é")
                throw new Exception("Atomic replacement or UTF-8 failed.");
            PreferenceFiles.WriteLines(file, new[] { "one", "two" });
            if (!File.ReadAllLines(file).SequenceEqual(new[] { "one", "two" }))
                throw new Exception("Preference lines changed.");
            bool failed = false;
            try
            {
                PreferenceFiles.WriteText(Path.Combine(file, "blocked.txt"), "invalid");
            }
            catch (IOException) { failed = true; }
            if (!failed || File.ReadAllLines(file)[0] != "one")
                throw new Exception("Write failure damaged existing settings.");
            if (Directory.GetFiles(folder, "*.tmp").Length != 0)
                throw new Exception("Temporary preference files leaked.");
        }
    }
}
