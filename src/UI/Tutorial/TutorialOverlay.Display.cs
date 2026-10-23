using System;
using System.Drawing;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed partial class TutorialOverlay
    {
        private InteractiveGlass tutorialGlass;
        private float tutorialZoom = 1;
        private int Z(int value)
        {
            return (int)Math.Round(value * tutorialZoom);
        }
        private void RefreshDisplay()
        {
            StopPinAnimation();
            StopSuggestionAnimation();
            foreach (var frame in frames.Values)
                frame.Dispose();
            frames.Clear();
            var work = Screen.FromControl(source).WorkingArea;
            tutorialZoom = Math.Min(DisplaySettings.WindowScale, Math.Min(work.Width / 918f, work.Height / 570f));
            Size = new Size(Math.Min(work.Width, Z(918)), Math.Min(work.Height, Z(570)));
            Location = WindowZoom.FitLocation(Location, Size, work);
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9 * tutorialZoom));
            foreach (var control in new Control[] { bubble, counter, body, error, previous, next, skip, dontShow, progressStrip })
                UiFonts.Assign(control, UiFonts.Create("Segoe UI", 9 * tutorialZoom));
            foreach (Control control in progressStrip.Controls)
                UiFonts.Assign(control, UiFonts.Create("Segoe UI", 9 * tutorialZoom));
            UiFonts.Assign(heading, UiFonts.Create("Segoe UI", 13 * tutorialZoom, FontStyle.Bold));
            if (Visible && !loading)
                ShowStep();
        }
        private void RefreshTutorialGlass()
        {
            if (tutorialGlass != null)
            {
                tutorialGlass.Dispose();
                tutorialGlass = null;
            }
            if (Visible && IsHandleCreated && Opacity > 0 && TransparencyPreferences.Children)
                tutorialGlass = new InteractiveGlass(this);
        }
    }
}
