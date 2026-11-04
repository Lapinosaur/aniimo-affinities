using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed partial class TutorialOverlay : Form
    {
        private readonly MainForm source;
        private readonly bool persist;
        private readonly TutorialProgress progress;
        internal static readonly string[] Sections = { Language.T("Types & affinities"), Language.T("Effectiveness matrix"), Language.T("Affinity radar & views"), Language.T("Pin & locked overlay"), Language.T("Keyboard shortcuts"), Language.T("Display & appearance"), Language.T("Team planning"), Language.T("What's new"), Language.T("Ally suggestions"), Language.T("App updates") };
        private readonly int[] sectionIds;
        private readonly int[] progressSections;
        private readonly TutorialProgressStrip progressStrip;
        internal static int SectionForStep(int step)
        {
            return step >= 32 ? 9 : step >= 28 ? 8 : step >= 26 ? 3 : step >= 23 ? 7 : step >= 17 ? 6 : step == 16 ? -1 : step < 5 ? 0 : step < 7 ? 1 : step < 10 ? 2 : step < 12 ? 3 : step < 14 ? 4 : 5;
        }
        internal sealed class Frame : IDisposable
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
        private readonly Label counter = new Label(), heading = new Label(), body = new ShortcutLabel(), error = new Label();
        private readonly CheckBox dontShow = new RoundCheckBox();
        private readonly SettingsButton previous = new SettingsButton(), next = new SettingsButton(), skip = new SettingsButton();
        private int index;
        private bool loading;
        private int frameThemeRevision = -1;
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
        internal TutorialOverlay(MainForm source, bool persist = false)
        {
            this.source = source;
            this.persist = persist;
            progress = persist ? TutorialProgress.Load() : new TutorialProgress();
            var selectedSteps = Enumerable.Range(0, steps.Length).Where(i => i != 26 && i != 27).SelectMany(i => i == 10 ? new[] { 10, 26, 27 } : new[] { i }).Where(i => i != 16 && (progress.Sections & (1 << SectionForStep(i))) != 0).Concat(new[] { 16 }).ToArray();
            sectionIds = selectedSteps.Select(SectionForStep).ToArray();
            steps = selectedSteps.Select(i => steps[i]).ToArray();
            progressSections = sectionIds.Where(section => section >= 0).Distinct().ToArray();
            progressStrip = new TutorialProgressStrip(progressSections.Select(section => Sections[section]).ToArray(), JumpToTutorial);
            Controls.Add(progressStrip);
            index = progress.Pending ? Math.Min(progress.Step, steps.Length - 1) : 0;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            Text = Language.T("Guided tour");
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
            dontShow.Text = Language.T("Don't show this tutorial again");
            dontShow.Checked = TutorialPreferences.Hidden;
            dontShow.BackColor = Theme.Surface;
            dontShow.ForeColor = Theme.TextColor;
            dontShow.TabIndex = 0;
            bubble.Controls.Add(dontShow);
            previous.Text = Language.T("Previous");
            previous.TabIndex = 1;
            previous.Click += (s, e) => NavigateStep(-1);
            skip.Text = progress.Sections == TutorialProgress.News ? Language.T("Skip · Esc") : Language.T("Pause · Esc");
            skip.KeyBadges = true;
            skip.TabIndex = 2;
            skip.Click += (s, e) =>
            {
                if (persist && progress.Sections == TutorialProgress.News)
                {
                    progress.Pending = false;
                    try
                    {
                        progress.Save();
                    }
                    catch (IOException) { error.Text = Language.T("Unable to save. Try Skip again."); return; }
                    catch (UnauthorizedAccessException) { error.Text = Language.T("Unable to save. Try Skip again."); return; }
                }
                Close();
            };
            next.Text = Language.T("Next");
            next.Primary = true;
            next.TabIndex = 3;
            next.Click += (s, e) => NavigateStep(1);
            bubble.Controls.Add(previous);
            bubble.Controls.Add(skip);
            bubble.Controls.Add(next);
            AcceptButton = next;
            CancelButton = skip;
            bubble.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var outline = Drawing.Round(new RectangleF(1, 1, bubble.Width - 3, bubble.Height - 3), Theme.PanelRadius)) using (var pen = new Pen(Theme.Border)) e.Graphics.DrawPath(pen, outline); };
            Shown += (s, e) => { RefreshDisplay(); RefreshTutorialGlass(); };
            VisibleChanged += (s, e) => RefreshTutorialGlass();
            DisplaySettings.DisplayChanged += RefreshDisplay;
            TransparencyPreferences.Changed += RefreshTutorialGlass;
            Theme.Changed += RefreshTheme;
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
                    progress.Pending = false;
                    progress.Step = 0;
                    if (persist)
                        progress.Save();
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (IOException) { error.Text = Language.T("Unable to save. Try again or use Skip."); }
                catch (UnauthorizedAccessException) { error.Text = Language.T("Unable to save. Try again or use Skip."); }
                return;
            }
            int section = sectionIds[index];
            if (section >= 0)
                progress.ViewedSteps[section] |= 1 << sectionIds.Take(index).Count(value => value == section);
            if (direction > 0 && section >= 0 && (index + 1 == steps.Length || sectionIds[index + 1] != section))
            {
                int count = sectionIds.Count(value => value == section);
                if ((progress.ViewedSteps[section] & ((1 << count) - 1)) == (1 << count) - 1)
                    progress.Completed |= 1 << section;
                progress.SectionSteps[section] = 0;
            }
            index = Math.Max(0, Math.Min(steps.Length - 1, index + direction));
            ShowStep();
        }
        internal void JumpToTutorial(int point)
        {
            if (loading || point < 0 || point >= progressSections.Length)
                return;
            index = Array.IndexOf(sectionIds, progressSections[point]);
            ShowStep();
        }
        private void ShowStep()
        {
            if (loading)
                return;
            loading = true;
            try
            {
                if (frameThemeRevision != Theme.Revision)
                {
                    foreach (var cached in frames.Values)
                        cached.Dispose();
                    frames.Clear();
                    frameThemeRevision = Theme.Revision;
                }
                StopPinAnimation();
                StopSuggestionAnimation();
                var step = steps[index];
                if (!frames.ContainsKey(step.View))
                {
                    next.BeginBusy(Language.T("Loading…"));
                    try
                    {
                        frames.Add(step.View, CapturePreview(step.View));
                    }
                    finally { next.EndBusy(); }
                }
                counter.Text = Language.T("GUIDED TOUR  ·  ") + (index + 1) + " / " + steps.Length;
                heading.Text = step.Title;
                body.Text = step.Body;
                error.Text = "";
                int viewedSection = sectionIds[index];
                if (viewedSection >= 0)
                    progress.ViewedSteps[viewedSection] |= 1 << sectionIds.Take(index).Count(value => value == viewedSection);
                progressStrip.UpdateProgress(viewedSection < 0 ? progressSections.Length - 1 : Array.IndexOf(progressSections, viewedSection), progressSections.Select(section => (progress.Completed & (1 << section)) != 0).ToArray());
                if (persist)
                {
                    int section = sectionIds[index];
                    if (section >= 0)
                    {
                        progress.Started |= 1 << section;
                        progress.SectionSteps[section] = sectionIds.Take(index).Count(value => value == section);
                    }
                    progress.Step = index;
                    progress.Pending = true;
                    try
                    {
                        progress.Save();
                    }
                    catch (IOException) { error.Text = Language.T("Progress could not be saved. Try Next again."); }
                    catch (UnauthorizedAccessException) { error.Text = Language.T("Progress could not be saved: access denied."); }
                }
                previous.Enabled = index > 0;
                previous.Visible = index > 0;
                next.Text = index == steps.Length - 1 ? Language.T("Finish") : Language.T("Next");
                dontShow.Visible = index == steps.Length - 1;
                ArrangeStep();
                StartPinAnimation(step.View);
                StartSuggestionAnimation(step.View);
                Activate();
                if (dontShow.Visible)
                    dontShow.Focus();
                else
                    next.Focus();
                Invalidate();
            }
            finally
            {
                loading = false;
                if (frameThemeRevision != Theme.Revision)
                    RefreshTheme();
            }
        }
        private void RefreshTheme()
        {
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            bubble.BackColor = Theme.Surface;
            foreach (var label in new[] { counter, heading, body, error })
            {
                label.BackColor = Theme.Surface;
                label.ForeColor = label == counter ? Theme.AccentForeground : label == error ? Theme.Error : Theme.TextColor;
            }
            dontShow.BackColor = Theme.Surface;
            dontShow.ForeColor = Theme.TextColor;
            if (!loading && IsHandleCreated && !IsDisposed && Visible)
                BeginInvoke(new Action(() => { if (!IsDisposed && Visible) ShowStep(); }));
        }
        private Frame CapturePreview(string view)
        {
            if (view.StartsWith("update-", StringComparison.Ordinal))
                return CaptureUpdatePreview(view);
            if (view.StartsWith("radar-", StringComparison.Ordinal))
            {
                using (var demo = new RadarTutorialScene(source, view))
                    return demo.Capture();
            }
            if (view.StartsWith("suggest-", StringComparison.Ordinal))
            {
                using (var demo = new SuggestionTutorialScene(view))
                    return demo.Capture();
            }
            Form host;
            var selected = source.TutorialSelection;
            var keys = source.TutorialBindings;
            if (view.StartsWith("pin-", StringComparison.Ordinal))
                host = source.CreateTutorialPreview();
            else if (view == "main" || view == "team" || view == "team-radar")
                host = source.CreateTutorialPreview(view != "main", view == "team-radar");
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
                    settings.TutorialSection(view == "display" ? Language.T("Display") : view == "window" ? Language.T("Window & overlay") : Language.T("Types"));
                Application.DoEvents();
                var pinHost = host as PortableWindow;
                if (view.StartsWith("pin-", StringComparison.Ordinal))
                    pinHost.SetTutorialPinState(view);
                var frame = new Frame { Image = new Bitmap(host.Width, host.Height) };
                host.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, host.Size));
                frame.Targets["all"] = new Rectangle(8, 8, host.Width - 16, host.Height - 16);
                frame.Targets["content"] = new Rectangle(16, (int)(host.Height * .23), host.Width - 32, (int)(host.Height * .67));
                var main = host as MainForm;
                if (main != null)
                    foreach (string target in new[] { "types", "content", "matrix", "radar", "pin", "team", "team-tools" })
                        frame.Targets[target] = main.TutorialTarget(target);
                if (view == "radar")
                    frame.Targets["modes"] = new Rectangle((int)(784 * ((PortableWindow)host).AppliedWindowScale), (int)(38 * ((PortableWindow)host).AppliedWindowScale), (int)(110 * ((PortableWindow)host).AppliedWindowScale), (int)(54 * ((PortableWindow)host).AppliedWindowScale));
                if (settings != null)
                    frame.Targets["appearance"] = new Rectangle(392, 46, 44, 40);
                if (view.StartsWith("pin-", StringComparison.Ordinal))
                    frame.Targets["pin"] = pinHost.DrawTutorialPinState(frame.Image);
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
            float scale = Math.Min((ClientSize.Width - Z(16)) / (float)frame.Image.Width, (ClientSize.Height - Z(82)) / (float)frame.Image.Height);
            imageBounds = new Rectangle((ClientSize.Width - (int)(frame.Image.Width * scale)) / 2, Z(66) + (ClientSize.Height - Z(66) - (int)(frame.Image.Height * scale)) / 2, (int)(frame.Image.Width * scale), (int)(frame.Image.Height * scale));
            if (frame.Image.Width < frame.Image.Height && ClientSize.Width - imageBounds.Width >= Z(432))
                imageBounds.X = Z(16);
            Rectangle target;
            frame.Targets.TryGetValue(steps[index].Target, out target);
            spotlight = new Rectangle(imageBounds.X + (int)(target.X * scale), imageBounds.Y + (int)(target.Y * scale), (int)(target.Width * scale), (int)(target.Height * scale));
            int width = Math.Min(Z(400), ClientSize.Width - Z(32)), inner = width - Z(32);
            int textHeight = ShortcutText.Measure(body.Text, body.Font, inner) + Z(10);
            int headingHeight = TextRenderer.MeasureText(heading.Text, heading.Font, new Size(inner, 1000), TextFormatFlags.WordBreak).Height + Z(4);
            counter.SetBounds(Z(16), Z(14), inner, Z(20));
            heading.SetBounds(Z(16), Z(40), inner, headingHeight);
            body.SetBounds(Z(16), Z(46) + headingHeight, inner, textHeight);
            int y = body.Bottom + Z(6);
            dontShow.SetBounds(Z(16), y, inner, Z(28));
            if (dontShow.Visible)
                y += Z(32);
            error.SetBounds(Z(16), y, inner, Z(20));
            y += Z(24);
            previous.SetBounds(Z(16), y, Z(100), Z(32));
            skip.SetBounds(width - Z(232), y, Z(104), Z(32));
            next.SetBounds(width - Z(120), y, Z(104), Z(32));
            var available = new Size(ClientSize.Width, Math.Max(1, ClientSize.Height - Z(66)));
            var relativeTarget = spotlight;
            relativeTarget.Offset(0, -Z(66));
            var placed = PlaceBubble(available, relativeTarget, new Size(width, y + Z(48)));
            placed.Offset(0, Z(66));
            bubble.Bounds = placed;
            progressStrip.SetBounds(Z(8), Z(4), ClientSize.Width - Z(16), Z(58));
            progressStrip.BringToFront();
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
            using (var shade = new SolidBrush(Color.FromArgb(175, Theme.Background)))
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
                PaintPinGesture(g);
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
            {
                StopPinAnimation();
                StopSuggestionAnimation();
                pinAnimationTimer.Dispose();
                suggestionAnimationTimer.Dispose();
                Theme.Changed -= RefreshTheme;
                DisplaySettings.DisplayChanged -= RefreshDisplay;
                TransparencyPreferences.Changed -= RefreshTutorialGlass;
                if (tutorialGlass != null)
                {
                    tutorialGlass.Dispose();
                    tutorialGlass = null;
                }
                foreach (var frame in frames.Values)
                    frame.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
