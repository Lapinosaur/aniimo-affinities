using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TutorialOverlay : Form
    {
        private readonly MainForm source;
        private readonly TourStep[] steps ={
            new TourStep("main","all","Welcome to Aniimo Affinities","Explore how types interact, compare matchups and keep useful information over your game. This guided preview leaves your selections and settings untouched."),
            new TourStep("main","types","Choose Main and Sub","Click a type to make it Main, then another to add Sub. A third type replaces Sub. Click a selected type to remove it; the cross at the end clears both."),
            new TourStep("main","types","Preview and keyboard shortcuts","Hover a type for a temporary preview. Click to keep it. Small keyboard badges show your configured shortcuts. Main / Sub labels always identify the committed selection."),
            new TourStep("main","content","Read attack and defense","Amber shows your best available attack. Blue shows damage received by the combined types. ×1 is neutral; above ×1 means more damage, below ×1 means less."),
            new TourStep("main","content","Understand each affinity","Hover a side group to highlight the type responsible in the center. Hover a central type to highlight the groups it contributes to. This also works with two selected types."),
            new TourStep("main","matrix","Open the effectiveness matrix","A short click opens the matrix in its own window. The keyboard badge uses your rebind from Settings. Tooltips describe each action."),
            new TourStep("matrix","content","Compare rows and columns","Rows attack; columns defend. Choose a type to compare dual-type defenders. Violet means reduced damage, gray neutral and turquoise increased damage; stronger shades mark extreme values. Best matchups highlights useful choices."),
            new TourStep("radar","all","Read the affinity radar","Each axis represents a type. Amber is attack: higher deals more damage. Blue is defense: lower takes less damage. Alternating bands help compare levels on the shared scale. Hover the two icons below for explanations."),
            new TourStep("radar","modes","Combine or separate","Use these two exclusive choices to overlay both curves or view one radar for attack and one for defense. The type filters use the same keyboard shortcuts as the main window."),
            new TourStep("main","radar","Click or hold","A short click opens the view. Hold for 0.9 seconds to swap it into the main window. The button takes the previous view's icon. The small turquoise mark identifies a hold action; its contour fills while holding."),
            new TourStep("main","pin","Pin, then lock","Click Pin to stay on top with a transparent background. Hold it to lock a click-through overlay. The compact lock button unlocks it, and the drag handle moves it. Pin and Lock are never restored at startup."),
            new TourStep("main","pin","Resize the locked overlay","The locked overlay has two size targets. Hold a size for 0.9 seconds to apply it; a short click does nothing. Releasing early or leaving the button cancels. Move and Unlock remain available beside them."),
            new TourStep("window","content","Rebind window actions","Pin, Lock / unlock, Settings, Minimize, Matrix and Radar all have configurable shortcuts. Select a field, then activate it to capture a key. Delete clears it, Esc cancels capture, and duplicate bindings are rejected."),
            new TourStep("types","content","Rebind type shortcuts","Each type and Clear selection has its own shortcut. Tab navigates and Space activates a focused field. The keyboard hints appear during keyboard navigation. Save commits changes; Cancel discards them."),
            new TourStep("display","content","Choose how the app looks","Click a preview to change text size, window scale or keyboard badges. Main / Sub labels remain visible. Apply temporarily reveals the original view; keep the changes within 15 seconds or the previous settings return."),
            new TourStep("display","appearance","Theme and window controls","The appearance button cycles Dark, System and Light. Esc closes custom windows, Alt+F4 closes the active window, and the minus button minimizes. Your selected types, view layout and display preferences are remembered."),
            new TourStep("main","all","You're ready","You can restart this tour from the help icon in Settings. Check the option below to stop showing it at startup; the same option remains available in Settings.")
        };
        private sealed class Frame : IDisposable
        {
            internal Bitmap Image;
            internal Dictionary<string, Rectangle> Targets = new Dictionary<string, Rectangle>();
            public void Dispose()
            {
                Image.Dispose();
            }
        }
        private readonly Dictionary<string, Frame> frames = new Dictionary<string, Frame>();
        private readonly DrawingPanel bubble = new DrawingPanel();
        private readonly Label counter = new Label(), heading = new Label(), body = new Label(), error = new Label();
        private readonly CheckBox dontShow = new CheckBox();
        private readonly SettingsButton previous = new SettingsButton(), next = new SettingsButton(), skip = new SettingsButton();
        private int index;
        private bool loading;
        private Rectangle imageBounds, spotlight;
        internal int StepCount
        {
            get
            {
                return steps.Length;
            }
        }
        internal int StepIndex
        {
            get
            {
                return index;
            }
        }
        internal TutorialOverlay(MainForm source)
        {
            this.source = source;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            Text = "Guided tour";
            BackColor = Theme.Background;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            ForeColor = Theme.TextColor;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            var work = Screen.FromControl(source).WorkingArea;
            Size = new Size(Math.Min(work.Width, Math.Max(760, source.Width)), Math.Min(work.Height, Math.Max(570, source.Height)));
            Location = new Point(Math.Max(work.Left, Math.Min(source.Left, work.Right - Width)), Math.Max(work.Top, Math.Min(source.Top, work.Bottom - Height)));
            bubble.BackColor = Theme.Surface;
            bubble.Padding = new Padding(16);
            Controls.Add(bubble);
            counter.ForeColor = Theme.AccentForeground;
            UiFonts.Assign(heading, UiFonts.Create("Segoe UI", 13, FontStyle.Bold));
            foreach (var label in new[] { counter, heading, body, error })
            {
                label.BackColor = Theme.Surface;
                label.ForeColor = label == counter ? Theme.AccentForeground : label == error ? Theme.Error : Theme.TextColor;
                bubble.Controls.Add(label);
            }
            dontShow.Text = "Don't show this tutorial again";
            dontShow.Checked = TutorialPreferences.Hidden;
            dontShow.BackColor = Theme.Surface;
            dontShow.ForeColor = Theme.TextColor;
            dontShow.TabIndex = 0;
            bubble.Controls.Add(dontShow);
            previous.Text = "Previous";
            previous.TabIndex = 1;
            previous.Click += (s, e) => NavigateStep(-1);
            skip.Text = "Skip · Esc";
            skip.TabIndex = 2;
            skip.Click += (s, e) => Close();
            next.Text = "Next";
            next.Primary = true;
            next.TabIndex = 3;
            next.Click += (s, e) => NavigateStep(1);
            bubble.Controls.Add(previous);
            bubble.Controls.Add(skip);
            bubble.Controls.Add(next);
            AcceptButton = next;
            CancelButton = skip;
            bubble.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var outline = Drawing.Round(new RectangleF(1, 1, bubble.Width - 3, bubble.Height - 3), Theme.PanelRadius)) using (var pen = new Pen(Theme.Border)) e.Graphics.DrawPath(pen, outline); };
            Shown += (s, e) => ShowStep();
            Resize += (s, e) => { if (frames.ContainsKey(steps[index].View)) ArrangeStep(); };
        }
        internal void NavigateStep(int direction)
        {
            if (loading)
                return;
            if (direction > 0 && index == steps.Length - 1)
            {
                try
                {
                    TutorialPreferences.Save(dontShow.Checked);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (IOException) { error.Text = "Unable to save. Try again or use Skip."; }
                catch (UnauthorizedAccessException) { error.Text = "Unable to save. Try again or use Skip."; }
                return;
            }
            index = Math.Max(0, Math.Min(steps.Length - 1, index + direction));
            ShowStep();
        }
        private void ShowStep()
        {
            if (loading)
                return;
            loading = true;
            try
            {
                var step = steps[index];
                if (!frames.ContainsKey(step.View))
                    frames.Add(step.View, CapturePreview(step.View));
                counter.Text = "GUIDED TOUR  ·  " + (index + 1) + " / " + steps.Length;
                heading.Text = step.Title;
                body.Text = step.Body;
                error.Text = "";
                previous.Enabled = index > 0;
                previous.Visible = index > 0;
                next.Text = index == steps.Length - 1 ? "Finish" : "Next";
                dontShow.Visible = index == steps.Length - 1;
                ArrangeStep();
                Activate();
                if (dontShow.Visible)
                    dontShow.Focus();
                else
                    next.Focus();
                Invalidate();
            }
            finally { loading = false; }
        }
        private Frame CapturePreview(string view)
        {
            Form host;
            var selected = source.TutorialSelection;
            var keys = source.TutorialBindings;
            if (view == "main")
                host = source.CreateTutorialPreview();
            else if (view == "matrix")
                host = new MatrixDialog(selected.Sub < 0 ? -1 : selected.Main, keys, selected.Main, selected.Sub);
            else if (view == "radar")
                host = new RadarDialog(selected.Main, selected.Sub, keys);
            else
                host = new SettingsDialog(keys, () => selected.Main);
            using (host)
            {
                host.Opacity = 0;
                host.ShowInTaskbar = false;
                host.StartPosition = FormStartPosition.Manual;
                host.Location = Location;
                host.Show();
                Application.DoEvents();
                var settings = host as SettingsDialog;
                if (settings != null)
                    settings.TutorialSection(view == "display" ? "Display" : view == "window" ? "Window & overlay" : "Types");
                Application.DoEvents();
                var frame = new Frame { Image = new Bitmap(host.Width, host.Height) };
                host.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, host.Size));
                frame.Targets["all"] = new Rectangle(8, 8, host.Width - 16, host.Height - 16);
                frame.Targets["content"] = new Rectangle(16, (int)(host.Height * .23), host.Width - 32, (int)(host.Height * .67));
                var main = host as MainForm;
                if (main != null)
                    foreach (string target in new[] { "types", "content", "matrix", "radar", "pin" })
                        frame.Targets[target] = main.TutorialTarget(target);
                if (view == "radar")
                    frame.Targets["modes"] = new Rectangle((int)(784 * ((PortableWindow)host).AppliedWindowScale), (int)(38 * ((PortableWindow)host).AppliedWindowScale), (int)(110 * ((PortableWindow)host).AppliedWindowScale), (int)(54 * ((PortableWindow)host).AppliedWindowScale));
                if (settings != null)
                    frame.Targets["appearance"] = new Rectangle(392, 46, 44, 40);
                host.Hide();
                return frame;
            }
        }
        internal static Rectangle PlaceBubble(Size area, Rectangle target, Size size)
        {
            int gap = 16;
            var candidates = new[] { new Rectangle(gap, area.Height - size.Height - gap, size.Width, size.Height), new Rectangle(area.Width - size.Width - gap, area.Height - size.Height - gap, size.Width, size.Height), new Rectangle(gap, gap, size.Width, size.Height), new Rectangle(area.Width - size.Width - gap, gap, size.Width, size.Height) };
            return candidates.OrderBy(rect => { var overlap = Rectangle.Intersect(rect, target); return (long)overlap.Width * overlap.Height; }).First();
        }
        private void ArrangeStep()
        {
            var frame = frames[steps[index].View];
            float scale = Math.Min((ClientSize.Width - 16) / (float)frame.Image.Width, (ClientSize.Height - 16) / (float)frame.Image.Height);
            imageBounds = new Rectangle((ClientSize.Width - (int)(frame.Image.Width * scale)) / 2, (ClientSize.Height - (int)(frame.Image.Height * scale)) / 2, (int)(frame.Image.Width * scale), (int)(frame.Image.Height * scale));
            if (frame.Image.Width < frame.Image.Height && ClientSize.Width - imageBounds.Width >= 432)
                imageBounds.X = 16;
            Rectangle target;
            frame.Targets.TryGetValue(steps[index].Target, out target);
            spotlight = new Rectangle(imageBounds.X + (int)(target.X * scale), imageBounds.Y + (int)(target.Y * scale), (int)(target.Width * scale), (int)(target.Height * scale));
            int width = Math.Min(400, ClientSize.Width - 32), inner = width - 32;
            int textHeight = TextRenderer.MeasureText(body.Text, body.Font, new Size(inner, 1000), TextFormatFlags.WordBreak).Height + 10;
            int headingHeight = TextRenderer.MeasureText(heading.Text, heading.Font, new Size(inner, 1000), TextFormatFlags.WordBreak).Height + 4;
            counter.SetBounds(16, 14, inner, 20);
            heading.SetBounds(16, 40, inner, headingHeight);
            body.SetBounds(16, 46 + headingHeight, inner, textHeight);
            int y = body.Bottom + 6;
            dontShow.SetBounds(16, y, inner, 28);
            if (dontShow.Visible)
                y += 32;
            error.SetBounds(16, y, inner, 20);
            y += 24;
            previous.SetBounds(16, y, 100, 32);
            skip.SetBounds(width - 232, y, 104, 32);
            next.SetBounds(width - 120, y, 104, 32);
            bubble.Bounds = PlaceBubble(ClientSize, spotlight, new Size(width, y + 48));
            using (var shape = Drawing.Round(new RectangleF(0, 0, bubble.Width, bubble.Height), Theme.PanelRadius))
            {
                var old = bubble.Region;
                bubble.Region = new Region(shape);
                if (old != null)
                    old.Dispose();
            }
            bubble.BringToFront();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Frame frame;
            if (!frames.TryGetValue(steps[index].View, out frame))
                return;
            g.DrawImage(frame.Image, imageBounds);
            using (var shade = new SolidBrush(Color.FromArgb(175, Theme.Anthracite)))
                g.FillRectangle(shade, ClientRectangle);
            if (steps[index].Target != "all")
            {
                using (var hole = Drawing.Round(spotlight, 10))
                {
                    var state = g.Save();
                    g.SetClip(hole);
                    g.DrawImage(frame.Image, imageBounds);
                    g.Restore(state);
                    using (var pen = new Pen(Theme.Accent, 2))
                        g.DrawPath(pen, hole);
                }
                var start = new PointF(bubble.Left + bubble.Width / 2, bubble.Top + bubble.Height / 2);
                var end = new PointF(spotlight.Left + spotlight.Width / 2, spotlight.Top + spotlight.Height / 2);
                using (var pen = new Pen(Color.FromArgb(160, Theme.Accent), 1))
                    g.DrawLine(pen, start, end);
            }
        }
        protected override bool ProcessCmdKey(ref Message message, Keys key)
        {
            if (key == Keys.Escape)
            {
                Close();
                return true;
            }
            if (key == Keys.Right && !dontShow.Focused)
            {
                NavigateStep(1);
                return true;
            }
            if (key == Keys.Left && !dontShow.Focused)
            {
                NavigateStep(-1);
                return true;
            }
            return base.ProcessCmdKey(ref message, key);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var frame in frames.Values)
                    frame.Dispose();
            base.Dispose(disposing);
        }
    }
}
