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
        internal static readonly string[] Sections = { "Types & affinities", "Effectiveness matrix", "Affinity radar & views", "Pin & locked overlay", "Keyboard shortcuts", "Display & appearance", "Team planning", "What's new", "Ally suggestions" };
        private readonly int[] sectionIds;
        private readonly int[] progressSections;
        private readonly TutorialProgressStrip progressStrip;
        internal static int SectionForStep(int step)
        {
            return step >= 28 ? 8 : step >= 26 ? 3 : step >= 23 ? 7 : step >= 17 ? 6 : step == 16 ? -1 : step < 5 ? 0 : step < 7 ? 1 : step < 10 ? 2 : step < 12 ? 3 : step < 14 ? 4 : 5;
        }
        private readonly TourStep[] steps ={
            new TourStep("main","all","Welcome to Affinisaur","Explore how types interact, compare matchups and keep useful information over your game. This guided preview leaves your selections and settings untouched."),
            new TourStep("main","types","Choose Main and Sub","Click a type to make it Main, then another to add Sub. A third type replaces Sub. Click a selected type to remove it; the cross at the end clears both."),
            new TourStep("main","types","Preview and keyboard shortcuts","Hover a type for a temporary preview. Click to keep it. Small keyboard badges show your configured shortcuts. Main / Sub labels always identify the committed selection."),
            new TourStep("main","content","Read attack and defense","Amber shows your best available attack. Blue shows damage received by the combined types. ×1 is neutral; above ×1 means more damage, below ×1 means less."),
            new TourStep("main","content","Understand each affinity","Hover a side group to highlight the type responsible in the center. Hover a central type to highlight the groups it contributes to. This also works with two selected types."),
            new TourStep("main","matrix","Open the effectiveness matrix","A short click swaps the matrix into the main window. Hold for 0.9 seconds to open it in its own window. The keyboard badge uses your rebind from Settings. Tooltips describe each action."),
            new TourStep("matrix","content","Compare rows and columns","Rows attack; columns defend. Choose a type to compare dual-type defenders. Violet means reduced damage, gray neutral and turquoise increased damage; stronger shades mark extreme values. Best matchups highlights useful choices."),
            new TourStep("radar-read", "radars", "1 · Read Attack and Defense", "Each axis is an element. Attack: higher deals more damage. Defense: lower takes less damage. The real radars update as the demo selects another ally; your own team stays untouched."),
            new TourStep("radar-select", "radars", "2 · Select allies to compare", "Check the circle on an ally card to add its traces to both radars. Check another to compare them. Each card shows the same color as its traces. With no checks, the radars return to the current selection."),
            new TourStep("radar-hover", "radars", "3 · Follow one ally", "Hover a checked ally card: its traces stand out on both radars while the others fade. Move to another card to follow that ally. Leaving the cards restores all traces; hovering does not change your selection."),
            new TourStep("pin-normal","pin","1 · Find the Pin button","The pin has two actions: click to stay on top; hold for 1.2 seconds to lock. In normal mode, the window is interactive and can sit behind other apps."),
            new TourStep("pin-locked","pin","4 · Move, resize and unlock","Drag the move handle to reposition the overlay. Hold a size for 0.9 seconds to resize; release early to cancel. Click the lock to return to pinned, interactive mode. Click Pin again to return to normal. Startup always returns to normal."),
            new TourStep("window","content","Rebind window actions","Pin, Lock / unlock, Settings, Minimize, Matrix and Radar all have configurable shortcuts. Select a field, then activate it to capture a key. Delete clears it, Esc cancels capture, and conflicts offer Swap, Unbind the other action or Cancel."),
            new TourStep("types","content","Rebind type shortcuts","Each type and Clear selection has its own shortcut. Tab navigates and Space activates a focused field. Outside capture, hold a shortcut to illuminate its matching row and test it safely. Save commits changes; Cancel discards them."),
            new TourStep("display","content","Choose how the app looks","Click a preview to change text size, window scale or keyboard badges. Main / Sub labels remain visible. Apply temporarily reveals the original view; keep the changes within 15 seconds or the previous settings return."),
            new TourStep("display","appearance","Theme and window controls","The appearance button cycles Dark, System and Light. Esc closes custom windows, Alt+F4 closes the active window, and the minus button minimizes. Your selected types, view layout and display preferences are remembered."),
            new TourStep("main","all","You're ready","This tour is complete. Settings > Help & Tutorial records your completed tutorials. Play, resume or replay each tutorial individually. The option below controls startup only."),
            new TourStep("team","team","Build a useful comparison","Open the team editor to enter allies and opponents. Names are optional. Use only elements you know: empty opponents are excluded and missing second elements are never guessed."),
            new TourStep("team","team","Preview, then select","Hover an ally to preview its affinities temporarily. Click the card to keep the selection and accent strip. The checkbox is separate: it includes that ally in the main radars. Attack counts favorable opponents; defense counts threats."),
            new TourStep("team","team-tools","Place your team panel","Use the arrows to place the panel beside, above or below the main view. In locked mode, hold its compact panel button to toggle it. Selection and suggestion buttons are hidden while locked; your selected traces remain."),
            new TourStep("team","team","Apply a suggested ally","Empty cards show gray proposed types. Suggested coverage builds general coverage; Suggested counter targets known opposing members. Hover for the graphical breakdown, then use the sparkle button to apply. Applying restores the colors and reveals the comparison checkbox."),
            new TourStep("team","team","Read the projected team","Suggestions are planned in slot order. Before/after counts include filled allies AND proposals in earlier empty slots, even before you apply them. A first answer adds coverage; further answers add backups. Applying any slot recalculates the remaining proposals."),
            new TourStep("team-radar","all","Compare and investigate","Check several allies for separate colored traces on the main Attack and Defense radars; each card shows its trace color. Uncheck all to return to ordinary selection. In the editor, the threat report groups single types, dual types and members; hover for individual matchups."),
            new TourStep("suggest-coverage", "suggestion", "New in 0.8 · Interactive suggestions", "Move into a suggestion bubble to inspect its breakdown without a countdown. Apply directly, open Radar preview, or use the top-right button to browse all suggestions. Earlier proposals remain projections until applied."),
            new TourStep("suggest-radar", "suggestion", "New in 0.8 · Preview before applying", "Compare a proposed ally with your team on the real radars. Hover its legend to isolate its traces, then Apply from the preview. On the main radars, hover checked ally cards to highlight them. This demo does not change your team."),
            new TourStep("display", "content", "New in 0.8 · Make it yours", "Display now includes theme and frosted-glass cards for Lock, secondary windows and the main window. Hover previews temporarily; Apply saves. Help offers animated tutorials, clickable progress points, Play all and Play unfinished. Your settings stay unchanged here."),
            new TourStep("pin-pinned","pin","2 · Click to pin","A short click highlights Pin. The window stays above other apps, with clear space between its panels. Buttons and dragging still work. Click Pin again to return to the normal window."),
            new TourStep("pin-hold","pin","3 · Hold to lock","Hold Pin for 1.2 seconds. Its contour fills as you hold. Release early or move away to cancel. At completion, the overlay becomes click-through: clicks reach your game and the compact control bar remains available."),
            new TourStep("suggest-coverage", "suggestion", "1 · Suggested coverage", "With no known opponents, gray proposals improve general coverage and add backup answers. The demo opens the same graphical bubble as your cards: these are suggestions, not applied allies."),
            new TourStep("suggest-counter", "suggestion", "2 · Suggested counter", "When opponents are known, Counter targets their elements. Their icons appear in the bubble with attack, defense and projected answers. Unknown opponents are never guessed."),
            new TourStep("suggest-radar", "suggestion", "3 · Compare before applying", "The radar button opens the real comparison preview. The proposed ally has its own trace. Hover a legend to highlight it; your team remains unchanged until you apply."),
            new TourStep("suggest-apply", "suggestion", "4 · Apply and recalculate", "The sparkle applies both proposed elements. Gray icons become colored and the checkbox replaces the suggestion action. Remaining slots recalculate complementary choices. Earlier proposals are included in projected counts, but are not applied automatically.")
        };
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
            skip.Text = progress.Sections == TutorialProgress.News ? "Skip · Esc" : "Pause · Esc";
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
                    catch (IOException) { error.Text = "Unable to save. Try Skip again."; return; }
                    catch (UnauthorizedAccessException) { error.Text = "Unable to save. Try Skip again."; return; }
                }
                Close();
            };
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
                catch (IOException) { error.Text = "Unable to save. Try again or use Skip."; }
                catch (UnauthorizedAccessException) { error.Text = "Unable to save. Try again or use Skip."; }
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
                    next.BeginBusy("Loading…");
                    try
                    {
                        frames.Add(step.View, CapturePreview(step.View));
                    }
                    finally { next.EndBusy(); }
                }
                counter.Text = "GUIDED TOUR  ·  " + (index + 1) + " / " + steps.Length;
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
                    catch (IOException) { error.Text = "Progress could not be saved. Try Next again."; }
                    catch (UnauthorizedAccessException) { error.Text = "Progress could not be saved: access denied."; }
                }
                previous.Enabled = index > 0;
                previous.Visible = index > 0;
                next.Text = index == steps.Length - 1 ? "Finish" : "Next";
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
                    settings.TutorialSection(view == "display" ? "Display" : view == "window" ? "Window & overlay" : "Types");
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
