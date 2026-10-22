using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class UnlockWindow : Form
    {
        protected override bool ProcessCmdKey(ref Message message, Keys input)
        {
            if (WindowZoom.HandleShortcut(input, message.LParam))
            {
                RefreshSizes();
                return true;
            }
            return base.ProcessCmdKey(ref message, input);
        }
        private readonly Action closeApplication;
        private readonly HoldSizeButton[] sizeButtons = new HoldSizeButton[2];
        internal void RefreshSizes()
        {
            var values = WindowZoom.Neighbors(DisplaySettings.WindowScale);
            for (int i = 0; i < 2; i++)
            {
                var button = sizeButtons[i];
                if (button == null)
                    continue;
                button.Target = values[i];
                button.Text = Math.Round(values[i] * 100) + "%";
                button.AccessibleName = "Resize overlay to " + button.Text;
                tips.SetToolTip(button, "Hold for 0.9 s to resize to " + button.Text + ". Release early to cancel.");
                button.Invalidate();
            }
        }
        private readonly ApplicationTips tips = ApplicationTips.Create();
        internal UnlockWindow(Action unlock, Action close, bool hotkeyAvailable, Func<Point> getLocation, Action<Point> move)
        {
            closeApplication = close;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Theme.Surface;
            ClientSize = new Size(172, 32);
            var shape = new Region();
            shape.MakeEmpty();
            foreach (var bounds in new[] { new RectangleF(0, 0, 48, 32), new RectangleF(52, 0, 48, 32), new RectangleF(104, 0, 32, 32), new RectangleF(140, 0, 32, 32) })
                using (var outline = Drawing.Round(bounds, 8))
                    shape.Union(outline);
            Region = shape;
            for (int i = 0; i < 2; i++)
            {
                var sizeButton = new HoldSizeButton { Location = new Point(i * 52, 0), Size = new Size(48, 32), TabIndex = i };
                sizeButtons[i] = sizeButton;
                Controls.Add(sizeButton);
                sizeButton.Commit += value =>
                {
                    try
                    {
                        DisplaySettings.SaveDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, value);
                        RefreshSizes();
                    }
                    catch (System.IO.IOException) { tips.SetToolTip(sizeButton, "Unable to save window size. Hold to try again."); }
                    catch (UnauthorizedAccessException) { tips.SetToolTip(sizeButton, "Unable to save window size. Hold to try again."); }
                };
            }
            RefreshSizes();
            var button = new Button { Location = new Point(140, 0), Size = new Size(32, 32), FlatStyle = FlatStyle.Flat, Text = "", AccessibleName = "Unlock overlay", Cursor = Cursors.Hand, BackColor = Theme.AccentSurface };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            button.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Glyphs.Draw(e.Graphics, "lock", new RectangleF(8, 8, 16, 16), Theme.AccentForeground);
            };
            button.Click += (s, e) => unlock();
            Controls.Add(button);
            tips.SetToolTip(button, hotkeyAvailable ? "Unlock overlay · " + KeyBindings.Label(KeyBindings.Load(KeyBindings.FilePath)[11]) : "Unlock overlay");
            var grip = new Button { Location = new Point(104, 0), Size = new Size(32, 32), FlatStyle = FlatStyle.Flat, Text = "", AccessibleName = "Move overlay", Cursor = Cursors.SizeAll, BackColor = Theme.Surface };
            grip.FlatAppearance.BorderSize = 0;
            grip.FlatAppearance.MouseOverBackColor = Theme.Surface;
            grip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(Theme.TextColor, 1.5f))
                {
                    e.Graphics.DrawLine(pen, 8, 16, 24, 16);
                    e.Graphics.DrawLine(pen, 16, 8, 16, 24);
                    e.Graphics.DrawLines(pen, new[] { new Point(11, 13), new Point(8, 16), new Point(11, 19) });
                    e.Graphics.DrawLines(pen, new[] { new Point(21, 13), new Point(24, 16), new Point(21, 19) });
                    e.Graphics.DrawLines(pen, new[] { new Point(13, 11), new Point(16, 8), new Point(19, 11) });
                    e.Graphics.DrawLines(pen, new[] { new Point(13, 21), new Point(16, 24), new Point(19, 21) });
                }
            };
            bool dragging = false;
            Point pointerStart = Point.Empty, windowStart = Point.Empty;
            grip.MouseDown += (s, e) => { if (e.Button != MouseButtons.Left) return; pointerStart = Cursor.Position; windowStart = getLocation(); dragging = true; grip.Capture = true; };
            grip.MouseMove += (s, e) => { if (dragging) move(new Point(windowStart.X + Cursor.Position.X - pointerStart.X, windowStart.Y + Cursor.Position.Y - pointerStart.Y)); };
            grip.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) { dragging = false; grip.Capture = false; } };
            grip.MouseCaptureChanged += (s, e) => { if (!grip.Capture) dragging = false; };
            Controls.Add(grip);
            tips.SetToolTip(grip, "Hold and drag to move overlay");
        }
        protected override void OnDeactivate(EventArgs e)
        {
            foreach (var button in sizeButtons)
                button.CancelHold();
            base.OnDeactivate(e);
        }
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                closeApplication();
                return;
            }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
