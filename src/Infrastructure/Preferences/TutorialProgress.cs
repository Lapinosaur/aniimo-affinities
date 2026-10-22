using System;
using System.IO;

namespace AniimoAffinities
{
    internal sealed class TutorialProgress
    {
        internal const int All = 63;
        internal int Sections = All;
        internal int Step;
        internal bool Pending;
        internal static readonly string FilePath = PreferenceFiles.FilePath("tutorial-progress.txt");
        internal static TutorialProgress Load(string path = null)
        {
            var result = new TutorialProgress();
            try
            {
                var lines = File.ReadAllLines(path ?? FilePath);
                int sections, step;
                if (lines.Length == 4 && lines[0] == "tour-v1" && int.TryParse(lines[1], out sections) && sections > 0 && sections <= All && int.TryParse(lines[2], out step) && step >= 0 && step < 17 && (lines[3] == "pending" || lines[3] == "done"))
                {
                    result.Sections = sections;
                    result.Step = step;
                    result.Pending = lines[3] == "pending";
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return result;
        }
        internal void Save(string path = null)
        {
            PreferenceFiles.WriteText(path ?? FilePath, "tour-v1\n" + Sections + "\n" + Step + "\n" + (Pending ? "pending" : "done"));
        }
    }
}
