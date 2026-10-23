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
        private readonly Timer revealTimer = new Timer { Interval = 80 };
        private Control keyboardReveal, lockButton;
        private UnlockAppearance appearance;
        private int revealMask = -1;
        private readonly Action closeApplication;
        internal float ButtonOpacity(Control control)
        {
            return control == lockButton || control.Capture || control == keyboardReveal && control.Focused
                || control.ClientRectangle.Contains(control.PointToClient(Cursor.Position)) ? 1 : .18f;
        }
        private void RefreshReveal()
        {
            int mask = 0, bit = 1;
            foreach (Control control in Controls)
            {
                if (ButtonOpacity(control) == 1)
                    mask |= bit;
                bit <<= 1;
            }
            if (mask == revealMask)
                return;
            revealMask = mask;
            if (appearance != null)
                appearance.Queue();
        }
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
                tips.SetToolTip(button, "Hold click or Space for 0.9 s to resize to " + button.Text + ". Release early to cancel.");
                button.Invalidate();
            }
        }
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private HoldSizeButton panelToggle;
        internal UnlockWindow(Action unlock, Action close, bool hotkeyAvailable, Func<Point> getLocation, Action<Point> move, Action togglePanel = null, Func<string> panelGlyph = null)
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
            var button = new SettingsButton { Location = new Point(140, 0), Size = new Size(32, 32), Glyph = "lock", AccessibleName = "Unlock overlay", Cursor = Cursors.Hand, Primary = true };
            lockButton = button;
            button.Click += (s, e) => unlock();
            Controls.Add(button);
            tips.SetToolTip(button, hotkeyAvailable ? "Unlock overlay · " + ShortcutText.Mark(KeyBindings.Label(KeyBindings.Load(KeyBindings.FilePath)[11])) : "Unlock overlay");
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
            grip.PreviewKeyDown += (s, e) => { if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) e.IsInputKey = true; };
            grip.KeyDown += (s, e) =>
            {
                int distance = e.Shift ? 10 : 1;
                int x = e.KeyCode == Keys.Left ? -distance : e.KeyCode == Keys.Right ? distance : 0;
                int y = e.KeyCode == Keys.Up ? -distance : e.KeyCode == Keys.Down ? distance : 0;
                if (x == 0 && y == 0)
                    return;
                var location = getLocation();
                move(new Point(location.X + x, location.Y + y));
                e.Handled = e.SuppressKeyPress = true;
            };
            Controls.Add(grip);
            tips.SetToolTip(grip, "Drag to move the overlay, or focus this button and use arrow keys. Shift moves 10 pixels.");
            if (togglePanel != null)
            {
                foreach (Control control in Controls)
                    control.Left += 36;
                ClientSize = new Size(208, 32);
                var expandedRegion = Region.Clone();
                expandedRegion.Translate(36, 0);
                using (var outline = Drawing.Round(new RectangleF(0, 0, 32, 32), 8))
                    expandedRegion.Union(outline);
                var oldRegion = Region;
                Region = expandedRegion;
                oldRegion.Dispose();
                panelToggle = new HoldSizeButton { Location = Point.Empty, Size = new Size(32, 32), Glyph = panelGlyph(), AccessibleName = "Hold to show or hide team panel" };
                panelToggle.Commit += value => { togglePanel(); panelToggle.Glyph = panelGlyph(); panelToggle.Invalidate(); };
                Controls.Add(panelToggle);
                tips.SetToolTip(panelToggle, "Hold click or Space for 0.9 s to show or hide the team panel. Release early to cancel.");
            }

            foreach (Control control in Controls)
            {
                control.MouseEnter += (s, e) => RefreshReveal();
                control.MouseDown += (s, e) => { keyboardReveal = null; RefreshReveal(); };
                control.GotFocus += (s, e) => { keyboardReveal = MouseButtons == MouseButtons.None ? control : null; RefreshReveal(); };
                control.KeyDown += (s, e) => { keyboardReveal = control; RefreshReveal(); };
            }
            Shown += (s, e) => { if (Opacity != 0) appearance = new UnlockAppearance(this); };
            revealTimer.Tick += (s, e) => RefreshReveal();
            VisibleChanged += (s, e) => { revealTimer.Enabled = Visible; if (Visible) RefreshReveal(); };
        }
        protected override void OnDeactivate(EventArgs e)
        {
            if (panelToggle != null)
                panelToggle.CancelHold();
            foreach (var button in sizeButtons)
                button.CancelHold();
            keyboardReveal = null;
            RefreshReveal();
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
            {
                revealTimer.Dispose();
                if (appearance != null)
                    appearance.Dispose();
                tips.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
