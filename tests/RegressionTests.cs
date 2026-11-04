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
        // ui-prefixed batches share an exclusive desktop lease in the runner.
        // Other batches must never show windows or use foreground input.
        private static readonly Suite[] Suites = {
            new Suite("Updates", "storage", UpdateTests.Run),
            new Suite("Resources", "uilifecycle", ResourceTests.Run),
            new Suite("Control lifetimes", "controls", LifetimeTests.Run),
            new Suite("Packaging", "storage", PackagingTests.Run),
            new Suite("PreferenceFiles", "storage", PreferenceFilesTests.Run),
            new Suite("Tutorial", "uitutorial", TutorialTests.Run),
            new Suite("Startup", "uilifecycle", StartupTests.Run),
            new Suite("Session", "storage", SessionTests.Run),
            new Suite("Settings", "uisettings", SettingsTests.Run),
            new Suite("BusyFeedback", "uiinput", BusyFeedbackTests.Run),
            new Suite("Accessibility", "uiinput", AccessibilityTests.Run),
            new Suite("HoldControls", "uiinput", HoldControlsTests.Run),
            new Suite("Zoom", "uiwindows", ZoomTests.Run),
            new Suite("ViewSwitching", "uiwindows", ViewSwitchingTests.Run),
            new Suite("Teams", "uiteams", TeamTests.Run),
            new Suite("Suggestions", "uiteams", SuggestionTests.Run),
            new Suite("BubbleClicks", "uiteams", BubbleClickTests.Run),
            new Suite("DisplayRollback", "uisettings", DisplayRollbackTests.Run),
            new Suite("WindowAttention", "uiwindows", WindowAttentionTests.Run),
            new Suite("Radar", "uiteams", RadarTests.Run),
            new Suite("Comparisons", "uiteams", ComparisonTests.Run),
            new Suite("Matrix", "controls", MatrixTests.Run),
            new Suite("Theme", "rendering", ThemeTests.Run),
            new Suite("LockedGlass", "uiglass", GlassTests.Run),
            new Suite("Transparency", "uiglass", TransparencyTests.Run),
            new Suite("WindowBounds", "model", WindowBoundsTests.Run),
            new Suite("Keyboard", "uiinput", KeyboardTests.Run),
            new Suite("Matchups", "model", MatchupsTests.Run),
            new Suite("CalculationParity", "model", CalculationParityTests.Run),
            new Suite("MainRender", "uilifecycle", MatchupsTests.RenderMain),
            new Suite("Onboarding", "uisettings", OnboardingTests.Run),
            new Suite("Support", "uisettings", SupportTests.Run),
            new Suite("Factory reset", "storage", FactoryResetTests.Run),
            new Suite("Localization", "uisettings", LocalizationTests.Run),
        };
        internal static string[] Groups()
        {
            return Suites.Select(suite => suite.Group).Distinct().ToArray();
        }
        internal static string Run(string group = "all")
        {
            if (Theme.Appearance != 1)
                throw new Exception("A fresh profile must default to the System theme");
            var selected = Suites.Where(suite => group == "all" || suite.Group == group || (group == "ui" && suite.Group.StartsWith("ui")) || suite.Name == group).ToArray();
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
