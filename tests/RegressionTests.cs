using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AniimoAffinities
{
    internal static class RegressionTests
    {
        private sealed class Suite
        {
            internal readonly string Name, Group;
            internal readonly Action Execute;
            internal Suite(string name, string group, Action execute)
            {
                Name = name;
                Group = group;
                Execute = execute;
            }
        }
        // Only groups without visible windows may overlap the single UI worker.
        private static readonly Suite[] Suites = {
            new Suite("Updates", "storage", UpdateTests.Run),
            new Suite("Resources", "ui", ResourceTests.Run),
            new Suite("Packaging", "storage", PackagingTests.Run),
            new Suite("PreferenceFiles", "storage", PreferenceFilesTests.Run),
            new Suite("Tutorial", "ui", TutorialTests.Run),
            new Suite("Startup", "ui", StartupTests.Run),
            new Suite("Session", "storage", SessionTests.Run),
            new Suite("Settings", "ui", SettingsTests.Run),
            new Suite("BusyFeedback", "ui", BusyFeedbackTests.Run),
            new Suite("Accessibility", "ui", AccessibilityTests.Run),
            new Suite("HoldControls", "ui", HoldControlsTests.Run),
            new Suite("Zoom", "ui", ZoomTests.Run),
            new Suite("ViewSwitching", "ui", ViewSwitchingTests.Run),
            new Suite("Teams", "ui", TeamTests.Run),
            new Suite("Suggestions", "ui", SuggestionTests.Run),
            new Suite("DisplayRollback", "ui", DisplayRollbackTests.Run),
            new Suite("WindowAttention", "ui", WindowAttentionTests.Run),
            new Suite("Radar", "ui", RadarTests.Run),
            new Suite("Comparisons", "ui", ComparisonTests.Run),
            new Suite("Matrix", "ui", MatrixTests.Run),
            new Suite("Theme", "ui", ThemeTests.Run),
            new Suite("LockedGlass", "ui", GlassTests.Run),
            new Suite("WindowBounds", "ui", WindowBoundsTests.Run),
            new Suite("Keyboard", "ui", KeyboardTests.Run),
            new Suite("Matchups", "model", MatchupsTests.Run),
            new Suite("CalculationParity", "model", CalculationParityTests.Run),
            new Suite("MainRender", "ui", MatchupsTests.RenderMain),
        };
        internal static string[] Groups()
        {
            return Suites.Select(suite => suite.Group).Distinct().ToArray();
        }
        internal static string Run(string group = "all")
        {
            if (Theme.Appearance != 1)
                throw new Exception("A fresh profile must default to the System theme");
            var selected = Suites.Where(suite => group == "all" || suite.Group == group).ToArray();
            if (selected.Length == 0)
                throw new ArgumentException("Unknown test group: " + group);
            var report = new StringBuilder();
            foreach (var suite in selected)
            {
                Console.WriteLine("START " + suite.Name);
                var clock = Stopwatch.StartNew();
                try
                {
                    suite.Execute();
                }
                catch (Exception error)
                {
                    throw new Exception("FAIL " + suite.Name + " (" + clock.ElapsedMilliseconds + " ms)\n" + report, error);
                }
                string result = "PASS " + suite.Name + " (" + clock.ElapsedMilliseconds + " ms)";
                Console.WriteLine(result);
                report.AppendLine(result);
            }
            return report.ToString();
        }
    }
}
