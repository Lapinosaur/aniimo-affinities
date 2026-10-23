using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class TutorialTests
    {
        internal static void Run()
        {
            string tutorialTestPath = Path.Combine(Path.GetTempPath(), "aniimo-tutorial-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                if (TutorialPreferences.Load(tutorialTestPath))
                    throw new Exception("Tutorial must be offered before opt-out");
                TutorialPreferences.Save(true, tutorialTestPath);
                if (!TutorialPreferences.Load(tutorialTestPath))
                    throw new Exception("Tutorial opt-out was not saved");
                TutorialPreferences.Save(false, tutorialTestPath);
                if (TutorialPreferences.Load(tutorialTestPath))
                    throw new Exception("Tutorial opt-in was not saved");
            }
            finally { if (File.Exists(tutorialTestPath)) File.Delete(tutorialTestPath); }
            try
            {
                new TutorialProgress { Sections = 18, Step = 2, Pending = true }.Save(tutorialTestPath);
                var saved = TutorialProgress.Load(tutorialTestPath);
                if (saved.Sections != 18 || saved.Step != 2 || !saved.Pending)
                    throw new Exception("Selected tutorial progress was not restored");
                saved.Completed = 2;
                saved.Started = 18;
                saved.SectionSteps[4] = 1;
                saved.Save(tutorialTestPath);
                var restored = TutorialProgress.Load(tutorialTestPath);
                if (restored.Completed != 2 || restored.Started != 18 || restored.SectionSteps[4] != 1)
                    throw new Exception("Individual tutorial completion and checkpoints must persist.");
                File.WriteAllText(tutorialTestPath, "tour-v1\n2\n1\npending");
                restored = TutorialProgress.Load(tutorialTestPath);
                if (!restored.Pending || restored.Step != 1 || restored.Completed != 0)
                    throw new Exception("Legacy resume must survive without inventing completion history.");
                File.WriteAllText(tutorialTestPath, "tour-v2\n127\n18\npending\n63\n127\n0,0,0,0,0,0,1");
                restored = TutorialProgress.Load(tutorialTestPath);
                if (restored.Completed != 63 || restored.SectionSteps[6] != 1 || restored.SectionSteps[7] != 0 || restored.Started != 127)
                    throw new Exception("Adding the news tour must preserve seven-section user progress.");
                File.WriteAllText(tutorialTestPath, "tour-v1\n0\n999\npending");
                if (TutorialProgress.Load(tutorialTestPath).Pending || TutorialProgress.Load(tutorialTestPath).Sections != TutorialProgress.All)
                    throw new Exception("Invalid tutorial progress must safely reset");
            }
            finally { if (File.Exists(tutorialTestPath)) File.Delete(tutorialTestPath); }
            TutorialPreferences.Save(true);
            if (File.Exists(TutorialNews.FilePath))
                File.Delete(TutorialNews.FilePath);
            File.WriteAllText(PreferenceFiles.FilePath("team-features-tour.txt"), "returning");
            var upgraded = new TutorialProgress { Completed = TutorialProgress.All, Started = TutorialProgress.All, Sections = 2, Step = 1, Pending = true };
            upgraded.SectionSteps[1] = 1;
            upgraded.SectionSteps[7] = 2;
            upgraded.ViewedSteps[7] = 7;
            TutorialNews.PrepareProgress(upgraded);
            if (upgraded.Completed != (TutorialProgress.All & ~TutorialProgress.News) || upgraded.Sections != TutorialProgress.News || upgraded.Step != 0 || upgraded.Pending || upgraded.SectionSteps[1] != 1 || upgraded.SectionSteps[7] != 0 || upgraded.ViewedSteps[7] != 0)
                throw new Exception("The 0.8 news tour must reset only its own completion and preserve other checkpoints.");
            if (!TutorialNews.PrepareStartup() || !TutorialNews.ReturningUser || TutorialNews.PrepareStartup())
                throw new Exception("Returning users must be offered news once, even with old tutorials hidden.");
            using (var newsSource = new MainForm())
            {
                new TutorialProgress { Sections = TutorialProgress.News, Completed = 127 }.Save();
                using (var news = new TutorialOverlay(newsSource, true))
                {
                    news.Show();
                    if (news.StepCount != 4)
                        throw new Exception("The news-only tour must contain three new-feature steps and its finish page.");
                    news.Controls.OfType<DrawingPanel>().SelectMany(panel => panel.Controls.OfType<SettingsButton>()).Single(button => button.Text == "Skip · Esc").PerformClick();
                    var skipped = TutorialProgress.Load();
                    if (skipped.Pending || skipped.Completed != 127 || TutorialNews.PrepareStartup())
                        throw new Exception("Skipping news must preserve old completion and prevent automatic reopening.");
                }
            }
            TutorialPreferences.Save(false);
            using (var source = new MainForm())
            {
                new TutorialProgress { Sections = 2 }.Save();
                using (var first = new TutorialOverlay(source, true))
                {
                    first.Show(source);
                    first.NavigateStep(1);
                    if (first.StepCount != 3)
                        throw new Exception("Only matrix steps and completion should be included");
                    first.Close();
                    if (TutorialProgress.Load().Completed != 0)
                        throw new Exception("Opening steps is not completion.");
                }
                using (var resumed = new TutorialOverlay(source, true))
                {
                    if (resumed.StepIndex != 1)
                        throw new Exception("Paused tour did not resume at the last step");
                    resumed.Show(source);
                    resumed.NavigateStep(1);
                    resumed.NavigateStep(1);
                    if (TutorialProgress.Load().Pending)
                        throw new Exception("Completed tour must clear resume state");
                    if (TutorialProgress.Load().Completed != 2)
                        throw new Exception("Only the finished tutorial should be checked: " + TutorialProgress.Load().Completed + " viewed " + string.Join(",", TutorialProgress.Load().ViewedSteps));
                }
                new TutorialProgress().Save();
            }
            using (var jumpSource = new MainForm())
            {
                new TutorialProgress().Save();
                using (var jumpTour = new TutorialOverlay(jumpSource, true))
                {
                    jumpTour.Show();
                    jumpTour.JumpToTutorial(2);
                    var jumped = TutorialProgress.Load();
                    if (jumped.Completed != 0 || jumped.ViewedSteps[1] != 0 || jumpTour.StepIndex != 7)
                        throw new Exception("Jumping ahead must not complete skipped tutorials.");
                    jumpTour.NavigateStep(1);
                    jumpTour.NavigateStep(1);
                    jumpTour.NavigateStep(1);
                    if (TutorialProgress.Load().Completed != 4)
                        throw new Exception("Only the visited radar tutorial should be checked after a jump.");
                    jumpTour.JumpToTutorial(0);
                    if (jumpTour.StepIndex != 0 || TutorialProgress.Load().Completed != 4)
                        throw new Exception("Jumping back must retain independent completion.");
                }
            }
            float displayFont = DisplaySettings.FontScale, displayZoom = DisplaySettings.WindowScale;
            bool displayBadges = DisplaySettings.ShowBadges;
            try
            {
                using (var displaySource = new MainForm())
                using (var displayTour = new TutorialOverlay(displaySource))
                {
                    displayTour.Show();
                    Application.DoEvents();
                    int fullWidth = displayTour.Width;
                    DisplaySettings.ApplyDisplay(.9f, false, .5f);
                    Application.DoEvents();
                    if (displayTour.Width >= fullWidth || displayTour.Controls.OfType<TutorialProgressStrip>().Any(strip => !displayTour.ClientRectangle.Contains(strip.Bounds)))
                        throw new Exception("Live Display changes must resize the tutorial and keep navigation visible.");
                    DisplaySettings.ApplyDisplay(1.1f, true, 1.5f);
                    Application.DoEvents();
                    if (!Screen.FromControl(displayTour).WorkingArea.Contains(displayTour.Bounds))
                        throw new Exception("Large tutorial must stay inside the screen.");
                }
            }
            finally { DisplaySettings.ApplyDisplay(displayFont, displayBadges, displayZoom); }
            var individual = new TutorialProgress { Completed = 1, Started = 3 };
            individual.SectionSteps[1] = 1;
            individual.Save();
            TutorialProgress launched = null;
            using (var settings = new SettingsDialog(KeyBindings.Defaults(), null, () => launched = TutorialProgress.Load()))
            {
                settings.Show();
                settings.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Tutorial").PerformClick();
                var controls = Descendants(settings).ToArray();
                var check = controls.OfType<RoundCheckBox>().Single(control => control.Text == "Types & affinities");
                if (!check.Checked || check.AutoCheck || check.TabStop)
                    throw new Exception("Completion check must be a read-only status.");
                controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Resume Effectiveness matrix").PerformClick();
                if (launched == null || launched.Sections != 2 || launched.Step != 1 || !launched.Pending || launched.Completed != 1)
                    throw new Exception("Individual resume must retain its checkpoint and other completion history.");
                controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Replay Types & affinities").PerformClick();
                if (launched.Sections != 1 || launched.Step != 0 || launched.Pending || launched.Completed != 1)
                    throw new Exception("Replay must restart only its own section without clearing completion.");
                controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Play unfinished tutorials").PerformClick();
                if (launched.Sections != (TutorialProgress.All & ~1) || launched.Step != 0 || launched.Pending || launched.Completed != 1)
                    throw new Exception("Unfinished batch must exclude completed tours and retain completion history.");
                controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Play all tutorials").PerformClick();
                if (launched.Sections != TutorialProgress.All || launched.Step != 0 || launched.Completed != 1)
                    throw new Exception("Play all must include every tutorial without clearing completion.");
            }
            string teamBefore = File.Exists(TeamPreferences.FilePath) ? File.ReadAllText(TeamPreferences.FilePath) : null;
            foreach (var view in new[] { "suggest-coverage", "suggest-counter", "suggest-radar", "suggest-apply" })
                using (var demo = new SuggestionTutorialScene(view))
                {
                    var proposal = TeamSuggestions.ComplementaryAdditions(demo.Plan)[3];
                    if (proposal == null || proposal.Counter != (view == "suggest-counter"))
                        throw new Exception("Suggestion tutorial must use the real coverage/counter context.");
                    demo.Advance(1);
                    using (var frame = demo.Capture())
                        if (!new Rectangle(Point.Empty, frame.Image.Size).Contains(frame.Targets["suggestion"]))
                            throw new Exception("Suggestion tutorial content must fit its capture.");
                    demo.Advance(2);
                    using (var frame = demo.Capture())
                        if (frame.Image.Width <= 0)
                            throw new Exception("Animation must capture its second phase.");
                    if (demo.Plan.Allies[3].Known != (view == "suggest-apply"))
                        throw new Exception("Only the application step may fill the demo slot.");
                }
            if ((File.Exists(TeamPreferences.FilePath) ? File.ReadAllText(TeamPreferences.FilePath) : null) != teamBefore)
                throw new Exception("Animated suggestion tutorial must not save a team.");
            using (var demoSource = new MainForm())
            {
                new TutorialProgress { Sections = 256 }.Save();
                using (var tour = new TutorialOverlay(demoSource, true))
                    if (tour.StepCount != 5)
                        throw new Exception("Suggestion tour must have four steps and a finish page.");
            }
            string radarTeamBefore = File.Exists(TeamPreferences.FilePath) ? File.ReadAllText(TeamPreferences.FilePath) : null;
            using (var radarSource = new MainForm())
            {
                var original = radarSource.TutorialSelection;
                foreach (string view in new[] { "radar-read", "radar-select", "radar-hover" })
                    using (var demo = new RadarTutorialScene(radarSource, view))
                    {
                        for (int phase = 0; phase < 3; phase++)
                        {
                            demo.Advance(phase);
                            int expected = view == "radar-select" ? phase : view == "radar-hover" ? 3 : 0;
                            if (demo.Host.TutorialComparisonCount != expected)
                                throw new Exception("Radar tutorial must update real comparison checkboxes.");
                            if (view == "radar-hover" && demo.Host.TutorialHighlightedSlot != phase - 1)
                                throw new Exception("Radar tutorial hover must reach the main radar highlight handler.");
                            using (var frame = demo.Capture())
                                if (!new Rectangle(Point.Empty, frame.Image.Size).Contains(frame.Targets["radars"]))
                                    throw new Exception("Radar tutorial controls must fit the captured window.");
                        }
                    }
                if (radarSource.TutorialSelection.Main != original.Main || radarSource.TutorialSelection.Sub != original.Sub)
                    throw new Exception("Radar tutorial modified the source selection.");
                new TutorialProgress { Sections = 4 }.Save();
                using (var tour = new TutorialOverlay(radarSource, true))
                    if (tour.StepCount != 4)
                        throw new Exception("Radar tour needs three demonstrations and a finish step.");
            }
            if ((File.Exists(TeamPreferences.FilePath) ? File.ReadAllText(TeamPreferences.FilePath) : null) != radarTeamBefore)
                throw new Exception("Radar tutorial must not persist its demonstration team.");
            float holdProgress;
            bool gesture;
            if (TutorialOverlay.PinAnimationState(false, 700, out holdProgress, out gesture) != "pin-normal" || !gesture ||
                TutorialOverlay.PinAnimationState(false, 900, out holdProgress, out gesture) != "pin-pinned" ||
                TutorialOverlay.PinAnimationState(true, 1200, out holdProgress, out gesture) != "pin-pinned" || Math.Abs(holdProgress - .5f) > .01 || !gesture ||
                TutorialOverlay.PinAnimationState(true, 1800, out holdProgress, out gesture) != "pin-locked" || holdProgress != 1 || gesture ||
                TutorialOverlay.PinAnimationState(true, 4000, out holdProgress, out gesture) != "pin-pinned" || holdProgress != 0)
                throw new Exception("Pin animation must show click, a 1.2-second hold, lock, then restart.");
            using (var lockSource = new MainForm())
            {
                string savedSession = File.Exists(SessionState.FilePath) ? File.ReadAllText(SessionState.FilePath) : null;
                new TutorialProgress { Sections = 1 << 3 }.Save();
                using (var lockTour = new TutorialOverlay(lockSource, true))
                {
                    if (lockTour.StepCount != 5)
                        throw new Exception("Lock tutorial must have four state previews and one finish step.");
                    lockTour.Show();
                    for (int i = 1; i < lockTour.StepCount; i++)
                    {
                        lockTour.NavigateStep(1);
                        if (i == 2)
                        {
                            var animation = System.Diagnostics.Stopwatch.StartNew();
                            while (animation.ElapsedMilliseconds < 2100)
                            {
                                Application.DoEvents();
                                System.Threading.Thread.Sleep(5);
                            }
                            var demo = (MainForm)typeof(TutorialOverlay).GetField("pinAnimationHost", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(lockTour);
                            if (demo == null || !demo.OverlayLocked || !demo.TutorialSnapshot)
                                throw new Exception("The running hold animation must reach the real locked demo state.");
                        }
                    }
                    if (typeof(TutorialOverlay).GetField("pinAnimationHost", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(lockTour) != null)
                        throw new Exception("Changing steps must dispose the animated demo window.");
                    if ((File.Exists(SessionState.FilePath) ? File.ReadAllText(SessionState.FilePath) : null) != savedSession)
                        throw new Exception("Pin previews must not persist session changes.");
                    if (lockSource.TopMost || lockSource.OverlayLocked)
                        throw new Exception("Lock tutorial must not change the user's window state.");
                }
            }
            new TutorialProgress().Save();
            var tourBubble = TutorialOverlay.PlaceBubble(new Size(760, 570), new Rectangle(10, 10, 500, 80), new Size(400, 260));
            if (!new Rectangle(0, 0, 760, 570).Contains(tourBubble) || tourBubble.IntersectsWith(new Rectangle(10, 10, 500, 80)))
                throw new Exception("Tutorial bubble must fit and avoid target when space is available");
            using (var tourSource = new MainForm())
            using (var tutorial = new TutorialOverlay(tourSource))
            {
                tourSource.Show();
                Application.DoEvents();
                tutorial.Show(tourSource);
                Application.DoEvents();
                var before = tourSource.TutorialSelection;
                int appearance = Theme.Appearance;
                foreach (int mode in new[] { 2, 0, appearance })
                {
                    Theme.Appearance = mode;
                    Theme.ApplyAppearance();
                    Application.DoEvents();
                    var bubble = tutorial.Controls.OfType<DrawingPanel>().Single();
                    if (tutorial.BackColor != Theme.Background || bubble.BackColor != Theme.Surface || bubble.Controls.OfType<Label>().Any(label => label.BackColor != Theme.Surface))
                        throw new Exception("Tutorial did not follow the current theme.");
                    var revision = typeof(TutorialOverlay).GetField("frameThemeRevision", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if ((int)revision.GetValue(tutorial) != Theme.Revision)
                        throw new Exception("Tutorial retained old-theme preview captures.");
                }
                for (int step = 1; step < tutorial.StepCount; step++)
                    tutorial.NavigateStep(1);
                if (tutorial.StepIndex != tutorial.StepCount - 1 || !tutorial.Controls.OfType<DrawingPanel>().SelectMany(panel => panel.Controls.OfType<CheckBox>()).Single().Visible)
                    throw new Exception("Final tutorial step must expose opt-out");
                tutorial.NavigateStep(-1);
                if (tutorial.StepIndex != tutorial.StepCount - 2)
                    throw new Exception("Tutorial previous action failed");
                if (tourSource.TutorialSelection.Main != before.Main || tourSource.TutorialSelection.Sub != before.Sub || tourSource.TopMost)
                    throw new Exception("Tutorial must preserve source state");
            }

        }
        private static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                yield return control;
                foreach (var child in Descendants(control))
                    yield return child;
            }
        }
    }
}
