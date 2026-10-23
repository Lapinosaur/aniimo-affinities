using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class MainForm : PortableWindow
    {
        private readonly Selection selection = new Selection();
        private readonly bool childView;
        protected override bool ContentOnly
        {
            get
            {
                return childView;
            }
        }
        private Keys[] bindings = KeyBindings.Load(KeyBindings.FilePath);
        private int hovered = -1;
        private readonly System.Collections.Generic.List<Tuple<RectangleF, int, int>> affinityHits = new System.Collections.Generic.List<Tuple<RectangleF, int, int>>();
        private readonly System.Collections.Generic.List<Tuple<RectangleF, int, int>> originHits = new System.Collections.Generic.List<Tuple<RectangleF, int, int>>();
        private int affinityMask, affinityColumn = -1;
        private bool hoveringOrigin;
        private Selection preview
        {
            get
            {
                if (teamPreview >= 0 && teamPlan != null)
                    return new Selection { Main = teamPlan.Allies[teamPreview].Main, Sub = teamPlan.Allies[teamPreview].Sub };
                return selection.Preview(hovered);
            }
        }
        private readonly IconButton[] types = new IconButton[9];
        private readonly TypeSelector row = new TypeSelector();
        private readonly DrawingPanel canvas = new DrawingPanel();
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly bool rememberSession;
        private void SaveSession()
        {
            if (TutorialSnapshot)
                return;
            if (rememberSession)
                new SessionState { Main = selection.Main, Sub = selection.Sub, Current = currentView, Radar = radarView, Matrix = matrixView }.Save(SessionState.FilePath);
        }
        internal MainForm(bool remember = false, bool child = false)
        {
            childView = child;
            Text = "Affinisaur";
            ClientSize = new Size(902, 500);
            MinimumSize = new Size(878, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            AutoScaleMode = AutoScaleMode.Dpi;
            row.Dock = DockStyle.Top;
            row.Height = 62;
            canvas.Dock = DockStyle.Fill;
            EnableWindowDrag(this);
            EnableWindowDrag(row);
            EnableWindowDrag(canvas);
            var reset = new IconButton { Glyph = "reset", Shortcut = "0", TabIndex = 9, Segment = true, SegmentEnd = true, Width = 40, Height = 36, AccessibleName = "Clear types" };
            reset.Location = new Point(13, 11);
            reset.Click += (s, e) => { hovered = -1; selection.Clear(); RefreshSelection(); };
            tips.SetToolTip(reset, "All elements");
            row.Controls.Add(reset);
            string[] order = { "Dark", "Earth", "Fire", "Grass", "Ice", "Light", "Lightning", "Water", "Wind" };
            foreach (string name in order)
            {
                int type = Array.IndexOf(Model.Names, name);
                var button = new IconButton { Type = type, Shortcut = (Array.IndexOf(order, name) + 1).ToString(), Segment = true, SegmentStart = name == "Dark", Height = 36, AccessibleName = name, TabIndex = Array.IndexOf(order, name) };
                button.Click += (s, e) => { hovered = -1; selection.Toggle(type); RefreshSelection(); };
                tips.SetToolTip(button, name);
                types[type] = button;
                row.Controls.Add(button);
                button.MouseEnter += (s, e) => { hovered = type; canvas.Invalidate(); };
                button.MouseLeave += (s, e) => { if (hovered == type) hovered = -1; canvas.Invalidate(); };
                // Focus indicates keyboard position; only actual pointer hover previews a type.
                button.GotFocus += (s, e) => button.Invalidate();
                button.LostFocus += (s, e) => button.Invalidate();
                button.AccessibleDescription = "Shortcut: " + button.Shortcut;
            }
            row.Resize += (s, e) => LayoutRow();
            canvas.Paint += PaintAffinities;
            canvas.MouseMove += (s, e) =>
            {
                var origin = originHits.FirstOrDefault(item => item.Item1.Contains(e.Location));
                var hit = origin ?? affinityHits.FirstOrDefault(item => item.Item1.Contains(e.Location));
                int mask = hit == null ? 0 : hit.Item2, column = hit == null ? -1 : hit.Item3;
                bool fromOrigin = origin != null;
                if (mask != affinityMask || column != affinityColumn || hoveringOrigin != fromOrigin)
                {
                    hoveringOrigin = fromOrigin;
                    affinityMask = mask;
                    affinityColumn = column;
                    canvas.Invalidate();
                }
            };
            canvas.MouseLeave += (s, e) => { hoveringOrigin = false; affinityMask = 0; affinityColumn = -1; canvas.Invalidate(); };
            Deactivate += (s, e) => { hovered = -1; canvas.Invalidate(); foreach (Control control in row.Controls) control.Invalidate(); };
            Activated += (s, e) => { hovered = -1; canvas.Invalidate(); foreach (Control control in row.Controls) control.Invalidate(); };
            Controls.Add(canvas);
            Controls.Add(row);
            CaptionContent = row;
            var saved = remember ? SessionState.Load(SessionState.FilePath) : new SessionState();
            selection.Main = saved.Main;
            selection.Sub = saved.Sub;
            currentView = saved.Current;
            radarView = saved.Radar;
            matrixView = saved.Matrix;
            inlineRadar = currentView == "radar";
            if (currentView == "matrix")
            {
                diagramSize = Size;
                Height = 680;
            }
            RefreshSelection();
            rememberSession = remember;
            RefreshShortcuts();
            InitializeTeam(remember);
            if (childView)
            {
                teamPlan.Visible = false;
                ApplyTeamLayout();
                teamToolbar.Visible = false;
                Text = "Affinity diagrams";
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
            }
        }
        internal Selection TutorialSelection
        {
            get
            {
                return new Selection { Main = selection.Main < 0 ? Array.IndexOf(Model.Names, "Grass") : selection.Main, Sub = selection.Main < 0 ? Array.IndexOf(Model.Names, "Water") : selection.Sub };
            }
        }
        internal Keys[] TutorialBindings
        {
            get
            {
                return (Keys[])bindings.Clone();
            }
        }
        internal MainForm CreateTutorialPreview(bool showTeam = false, bool compare = false)
        {
            var form = new MainForm { TutorialSnapshot = true };
            var selected = TutorialSelection;
            form.selection.Main = selected.Main;
            form.selection.Sub = selected.Sub;
            form.bindings = TutorialBindings;
            form.RefreshSelection();
            form.RefreshShortcuts();
            if (showTeam)
            {
                form.teamPlan.Allies[0].Main = selected.Main;
                form.teamPlan.Allies[0].Sub = selected.Sub;
                form.teamPlan.Opponents[0].Main = 0;
                form.teamPlan.Opponents[1].Main = 1;
                form.teamPlan.Visible = true;
                form.teamPlan.Position = "Left";
                form.teamBoard.RefreshCards();
                form.ApplyTeamLayout();
                form.FinishTeamTransition();
                if (compare)
                {
                    form.teamPlan.Allies[1].Main = 0;
                    form.teamPlan.Allies[1].Sub = 5;
                    form.teamBoard.RefreshCards();
                    form.ToggleInlineRadar();
                    foreach (var card in form.teamBoard.Controls.OfType<TeamCard>().Take(2))
                        card.CompareCheck.Checked = true;
                }
            }
            return form;
        }
        internal Rectangle TutorialTarget(string name)
        {
            Control target = name == "team" ? teamBoard : name == "team-tools" ? teamToolbar : name == "types" ? row : name == "content" ? canvas : TutorialCaptionTarget(name);
            return target == null ? ClientRectangle : RectangleToClient(target.RectangleToScreen(target.ClientRectangle));
        }
        private void StartTutorial()
        {
            using (var tour = new TutorialOverlay(this, true))
                tour.ShowDialog(Form.ActiveForm ?? this);
        }
        // Compose child HWNDs together: buffering only the form does not buffer caption buttons.
        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return parameters;
            }
        }
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // State changes resize around the top-right corner; explicit moves stay untouched.
            if (Visible && WindowState == FormWindowState.Normal && (specified & BoundsSpecified.Width) != 0 && (specified & BoundsSpecified.X) == 0)
            {
                width = Math.Max(width, MinimumSize.Width);
                x = Right - width;
                specified |= BoundsSpecified.X;
            }
            base.SetBoundsCore(x, y, width, height, specified);
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PrepareGlass();

            if (rememberSession)
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed || !Visible)
                        return;
                    var progress = TutorialProgress.Load();
                    try
                    {
                        if (TutorialNews.PrepareStartup())
                        {
                            TutorialNews.PrepareProgress(progress);
                            progress.Save();
                            StartTutorial();
                            return;
                        }
                        if (TutorialNews.ReturningUser || TutorialPreferences.Hidden)
                            return;
                    }
                    catch (System.IO.IOException error) { DiagnosticReport.Record(error); return; }
                    catch (UnauthorizedAccessException error) { DiagnosticReport.Record(error); return; }
                    if (!progress.Pending && progress.Completed == TutorialProgress.All)
                        return;
                    if (!progress.Pending)
                    {
                        progress.Sections = (TutorialProgress.All & ~TutorialProgress.News) & ~progress.Completed;
                        if (progress.Sections == 0)
                            return;
                        progress.Step = 0;
                        try
                        {
                            progress.Save();
                        }
                        catch (System.IO.IOException error) { DiagnosticReport.Record(error); return; }
                        catch (UnauthorizedAccessException error) { DiagnosticReport.Record(error); return; }
                    }
                    StartTutorial();
                }));
        }
        protected override void OpenSettings()
        {
            hovered = -1;
            canvas.Invalidate();
            using (var dialog = new SettingsDialog(bindings, () => selection.Main, StartTutorial))
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    bindings = dialog.Result;
                    RefreshShortcuts();
                }
        }
        protected override void OpenMatrix()
        {
            OpenView(matrixView);
        }
        protected override Keys MatrixShortcut
        {
            get
            {
                return bindings[14];
            }
        }
        protected override Keys RadarShortcut
        {
            get
            {
                return bindings[15];
            }
        }
        private bool inlineRadar;
        private string currentView = "affinities", radarView = "radar", matrixView = "matrix";
        private Size diagramSize;
        protected override string RadarViewGlyph
        {
            get
            {
                return radarView;
            }
        }
        protected override string MatrixViewGlyph
        {
            get
            {
                return matrixView;
            }
        }
        protected override bool InlineRadar
        {
            get
            {
                return inlineRadar;
            }
        }
        protected override void ToggleInlineRadar()
        {
            SwapView(ref radarView);
        }
        protected override void ToggleInlineMatrix()
        {
            SwapView(ref matrixView);
        }
        private void SwapView(ref string slot)
        {
            using (new VisualUpdate(this))
            {
                teamTransition.Finish();
                string previous = currentView;
                currentView = slot;
                slot = previous;
                inlineRadar = currentView == "radar";
                if (currentView == "matrix")
                {
                    diagramSize = new Size((int)(Width / AppliedWindowScale), (int)(Height / AppliedWindowScale));
                    Height = Math.Max(Height, (int)(614 * AppliedWindowScale) + affinityHeader.Height);
                }
                else if (previous == "matrix" && diagramSize.Width > 0)
                    Size = new Size((int)(diagramSize.Width * AppliedWindowScale), (int)(diagramSize.Height * AppliedWindowScale));
                hovered = -1;
                affinityMask = 0;
                RefreshRadarShortcut();
                RefreshMatrixShortcut();
                canvas.Invalidate();
                SaveSession();
                ApplyWindowScale();
            }
        }
        protected override void OpenRadar()
        {
            OpenView(radarView);
        }
        private void OpenView(string view)
        {
            hovered = -1;
            canvas.Invalidate();
            if (view == "radar")
                using (var dialog = new RadarDialog(selection.Main, selection.Sub, bindings))
                    dialog.ShowDialog(this);
            else if (view == "matrix")
                using (var dialog = new MatrixDialog(selection.Sub >= 0 ? selection.Main : -1, bindings, selection.Main, selection.Sub))
                    dialog.ShowDialog(this);
            else
                using (var dialog = new MainForm(false, true))
                {
                    dialog.selection.Main = selection.Main;
                    dialog.selection.Sub = selection.Sub;
                    dialog.bindings = (Keys[])bindings.Clone();
                    dialog.RefreshSelection();
                    dialog.RefreshShortcuts();
                    dialog.ShowDialog(this);
                }
        }
        protected override void OnOverlayLockChanged()
        {
            hovered = -1;
            teamPreview = -1;
            teamHovered = -1;
            teamCounter = -1;
            teamOpponentMask = 0;
            if (teamBoard != null)
                teamBoard.SetOverlayLocked(OverlayLocked);
            if (teamToolbar != null)
                teamToolbar.Visible = !OverlayLocked && !childView;
            RefreshOverlayHeader();
            canvas.Invalidate();
        }
        private void RefreshShortcuts()
        {
            RefreshMatrixShortcut();
            RefreshRadarShortcut();
            foreach (var button in row.Controls.OfType<IconButton>())
            {
                button.Shortcut = KeyBindings.Label(bindings[button.Type < 0 ? 0 : Array.IndexOf(KeyBindings.Actions, Model.Names[button.Type])]);
                button.AccessibleDescription = button.Shortcut.Length > 0 ? "Shortcut: " + button.Shortcut : "No shortcut";
                tips.SetToolTip(button, (button.Type < 0 ? "All elements" : Model.Names[button.Type]) + (button.Shortcut.Length > 0 ? " · " + ShortcutText.Mark(button.Shortcut) : ""));
                button.Invalidate();
            }
        }
        // Inputs: window-local key. Output: advertised digit or focused-row navigation. Fallback: native Tab/Space/Alt+F4. Raises: none.
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (childView && keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            int action = Array.IndexOf(bindings, keyData);
            if (action >= 10)
            {
                if (childView)
                    return true;
                if ((message.LParam.ToInt64() & (1L << 30)) == 0)
                {
                    if (action == 10)
                        SetPinned(!TopMost);
                    else if (action == 11)
                        SetOverlayLocked(!OverlayLocked);
                    else if (action == 12)
                        OpenSettings();
                    else if (action == 13)
                        WindowState = FormWindowState.Minimized;
                    else if (action == 14)
                        OpenMatrix();
                    else if (action == 15)
                        OpenRadar();
                }
                return true;
            }
            if (OverlayLocked)
                return base.ProcessCmdKey(ref message, keyData);
            Keys key = keyData & Keys.KeyCode, modifiers = keyData & Keys.Modifiers;
            if ((modifiers & (Keys.Control | Keys.Alt)) != 0)
                return base.ProcessCmdKey(ref message, keyData);
            var buttons = row.Controls.OfType<IconButton>().Where(button => button.Visible).OrderBy(button => button.TabIndex).ToArray();
            int digit = KeyBindings.Resolve(bindings, keyData);
            if (digit >= 0)
            {
                var target = row.Controls.OfType<IconButton>().FirstOrDefault(button => digit == 0 ? button.Type < 0 : button.Type >= 0 && Model.Names[button.Type] == KeyBindings.Actions[digit]);
                if (target != null && target.Visible && (message.LParam.ToInt64() & (1L << 30)) == 0)
                    target.PerformClick();
                return true;
            }
            int focused = Array.FindIndex(buttons, button => button.Focused);
            if (focused >= 0 && modifiers == Keys.None)
            {
                int next = key == Keys.Left ? (focused + buttons.Length - 1) % buttons.Length : key == Keys.Right ? (focused + 1) % buttons.Length : key == Keys.Home ? 0 : key == Keys.End ? buttons.Length - 1 : -1;
                if (next >= 0)
                {
                    buttons[next].Focus();
                    return true;
                }
                if (key == Keys.Enter)
                {
                    if ((message.LParam.ToInt64() & (1L << 30)) == 0)
                        buttons[focused].PerformClick();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref message, keyData);
        }
        // Inputs: current selection. Output: Main/Sub badges and accessible state. Fallback: empty prompt. Raises: none.
        private void RefreshSelection()
        {
            if (teamBoard != null)
                teamBoard.SyncSelection(selection.Main, selection.Sub);
            hoveringOrigin = false;
            affinityMask = 0;
            affinityColumn = -1;
            foreach (var button in types)
            {
                button.Badge = button.Type == selection.Main ? "Main" : button.Type == selection.Sub ? "Sub" : "";
                button.Active = button.Badge.Length > 0;
                button.AccessibleName = Model.Names[button.Type] + (button.Active ? " · " + button.Badge : "");
                button.Invalidate();
            }
            LayoutRow();
            canvas.AccessibleName = "Type affinities";
            canvas.AccessibleRole = AccessibleRole.Graphic;
            canvas.AccessibleText = () =>
            {
                var comparison = ComparisonSeries();
                return comparison.Length == 0 ? AccessibleMatchups.Describe(selection)
                    : string.Join("\n\n", comparison.Select(item => item.Member.Title(item.Slot) + ": " + AccessibleMatchups.Describe(new Selection { Main = item.Member.Main, Sub = item.Member.Sub })).ToArray());
            };
            canvas.Invalidate();
            SaveSession();
        }
        private void LayoutRow()
        {
            row.Arrange(selection.Main >= 0);
        }

    }
}
