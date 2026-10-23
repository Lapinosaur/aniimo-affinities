using System;
using System.IO;
using System.Linq;

namespace AniimoAffinities
{
    internal sealed class TutorialProgress
    {
        internal const int All = 255;
        internal const int News = 128;
        internal int Sections = All;
        internal int Step;
        internal bool Pending;
        internal int Completed, Started;
        internal readonly int[] SectionSteps = new int[8];
        internal static readonly string FilePath = PreferenceFiles.FilePath("tutorial-progress.txt");
        internal static TutorialProgress Load(string path = null)
        {
            var result = new TutorialProgress();
            try
            {
                var lines = File.ReadAllLines(path ?? FilePath);
                int sections, step;
                bool legacy = lines.Length == 4 && lines[0] == "tour-v1";
                bool current = lines.Length == 7 && lines[0] == "tour-v2";
                if ((legacy || current) && int.TryParse(lines[1], out sections) && sections > 0 && sections <= All && int.TryParse(lines[2], out step) && step >= 0 && step < 32 && (lines[3] == "pending" || lines[3] == "done"))
                {
                    if (current)
                    {
                        int completed, started;
                        var checkpoints = lines[6].Split(',');
                        if (!int.TryParse(lines[4], out completed) || completed < 0 || completed > All || !int.TryParse(lines[5], out started) || started < 0 || started > All || (checkpoints.Length != 7 && checkpoints.Length != 8))
                            return new TutorialProgress();
                        for (int i = 0; i < checkpoints.Length; i++)
                            if (!int.TryParse(checkpoints[i], out result.SectionSteps[i]) || result.SectionSteps[i] < 0 || result.SectionSteps[i] > 7)
                                return new TutorialProgress();
                        result.Completed = completed;
                        result.Started = started;
                    }
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
            PreferenceFiles.WriteText(path ?? FilePath, "tour-v2\n" + Sections + "\n" + Step + "\n" + (Pending ? "pending" : "done") + "\n" + Completed + "\n" + Started + "\n" + string.Join(",", SectionSteps.Select(value => value.ToString()).ToArray()));
        }
    }
}
