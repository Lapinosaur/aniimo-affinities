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
    }
}
