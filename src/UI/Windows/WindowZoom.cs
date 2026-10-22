using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace AniimoAffinities
{
    internal static class WindowZoom
    {
        internal static float Step(float scale, int direction)
        {
            return Math.Max(.5f, Math.Min(1.5f, scale + direction * .25f));
        }
        internal static bool HandleShortcut(Keys input, IntPtr flags)
        {
            if (input != (Keys.Control | Keys.Up) && input != (Keys.Control | Keys.Down))
                return false;
            if ((flags.ToInt64() & (1L << 30)) != 0)
                return true;
            float next = Step(DisplaySettings.WindowScale, input == (Keys.Control | Keys.Up) ? 1 : -1);
            if (next == DisplaySettings.WindowScale)
                return true;
            try
            {
                DisplaySettings.SaveDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, next);
            }
            catch (System.IO.IOException) { System.Media.SystemSounds.Exclamation.Play(); }
            catch (UnauthorizedAccessException) { System.Media.SystemSounds.Exclamation.Play(); }
            return true;
        }
        internal static float[] Neighbors(float scale)
        {
            if (scale <= .5f)
                return new[] { .75f, 1f };
            if (scale < 1f)
                return new[] { .5f, 1f };
            if (scale == 1f)
                return new[] { .5f, 1.5f };
            if (scale < 1.5f)
                return new[] { 1f, 1.5f };
            return new[] { 1f, 1.25f };
        }
        internal static float Factor(Control control)
        {
            var form = control.FindForm() as PortableWindow;
            return form == null ? 1 : form.AppliedWindowScale;
        }
        internal static void Apply(PortableWindow window, float scale)
        {
            var main = window as MainForm;
            if (main != null)
                main.FinishTeamTransition();
            var area = Screen.FromControl(window).WorkingArea;
            int anchoredRight = window.Right, anchoredTop = window.Top;
            scale = FitScale(scale, new SizeF(window.Width / window.AppliedWindowScale, window.Height / window.AppliedWindowScale), area.Size);
            float ratio = scale / window.AppliedWindowScale;
            if (Math.Abs(ratio - 1) < .001)
                return;
            using (new VisualUpdate(window))
            {
                var bounds = new Dictionary<Control, Rectangle>();
                Collect(window, bounds);
                Size size = window.ClientSize, min = window.MinimumSize;
                window.SuspendLayout();
                window.MinimumSize = Size.Empty;
                window.AppliedWindowScale = scale;
                foreach (var item in bounds)
                {
                    item.Key.Bounds = Scale(item.Value, ratio);
                    if (item.Key is Label)
                    {
                        UiFonts.Assign(item.Key, new Font(item.Key.Font.FontFamily, item.Key.Font.Size * ratio, item.Key.Font.Style));
                        var padding = item.Key.Padding;
                        item.Key.Padding = new Padding((int)Math.Round(padding.Left * ratio), (int)Math.Round(padding.Top * ratio), (int)Math.Round(padding.Right * ratio), (int)Math.Round(padding.Bottom * ratio));
                    }
                }
                window.ClientSize = new Size((int)Math.Round(size.Width * ratio), (int)Math.Round(size.Height * ratio));
                window.MinimumSize = new Size((int)Math.Round(min.Width * ratio), (int)Math.Round(min.Height * ratio));
                window.ResumeLayout(true);
                Arrange(window);
                window.Invalidate(true);
                var desired = window is MainForm ? new Point(anchoredRight - window.Width, anchoredTop) : window.Location;
                window.Location = FitLocation(desired, window.Size, area);
            }
        }
        internal static Point FitLocation(Point desired, Size size, Rectangle work)
        {
            return new Point(Math.Max(work.Left, Math.Min(desired.X, work.Right - size.Width)), Math.Max(work.Top, Math.Min(desired.Y, work.Bottom - size.Height)));
        }
        internal static float FitScale(float requested, SizeF logicalSize, Size screen)
        {
            return Math.Min(requested, Math.Min((screen.Width - 16) / Math.Max(1, logicalSize.Width), (screen.Height - 16) / Math.Max(1, logicalSize.Height)));
        }
        private static Rectangle Scale(Rectangle r, float scale)
        {
            return new Rectangle((int)Math.Round(r.X * scale), (int)Math.Round(r.Y * scale), (int)Math.Round(r.Width * scale), (int)Math.Round(r.Height * scale));
        }
        private static void Collect(Control parent, Dictionary<Control, Rectangle> bounds)
        {
            foreach (Control child in parent.Controls)
            {
                bounds[child] = child.Bounds;
                Collect(child, bounds);
            }
        }
        private static void Arrange(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                var row = child as TypeSelector;
                if (row != null)
                {
                    bool selected = false;
                    foreach (Control button in row.Controls)
                        if (button is IconButton && ((IconButton)button).Type < 0 && button.Visible && !row.IconModes)
                            selected = true;
                    row.Arrange(selected);
                }
                Arrange(child);
            }
        }
    }
}
