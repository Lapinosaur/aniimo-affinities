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
                File.WriteAllText(tutorialTestPath, "tour-v1\n0\n999\npending");
                if (TutorialProgress.Load(tutorialTestPath).Pending || TutorialProgress.Load(tutorialTestPath).Sections != TutorialProgress.All)
                    throw new Exception("Invalid tutorial progress must safely reset");
            }
            finally { if (File.Exists(tutorialTestPath)) File.Delete(tutorialTestPath); }
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
                        throw new Exception("Only the finished tutorial should be checked.");
                }
                new TutorialProgress().Save();
            }
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
