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
            if (rememberSession)
                new SessionState { Main = selection.Main, Sub = selection.Sub, Current = currentView, Radar = radarView, Matrix = matrixView }.Save(SessionState.FilePath);
        }
        internal MainForm(bool remember = false)
        {
            Text = "Aniimo Affinities";
            ClientSize = new Size(902, 500);
            MinimumSize = new Size(878, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            Font = UiFonts.Create("Segoe UI", 9);
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
        internal MainForm CreateTutorialPreview()
        {
            var form = new MainForm();
            var selected = TutorialSelection;
            form.selection.Main = selected.Main;
            form.selection.Sub = selected.Sub;
            form.bindings = TutorialBindings;
            form.RefreshSelection();
            form.RefreshShortcuts();
            return form;
        }
        internal Rectangle TutorialTarget(string name)
        {
            Control target = name == "types" ? row : name == "content" ? canvas : TutorialCaptionTarget(name);
            return target == null ? ClientRectangle : RectangleToClient(target.RectangleToScreen(target.ClientRectangle));
        }
        private void StartTutorial()
        {
            using (var tour = new TutorialOverlay(this))
                tour.ShowDialog(Form.ActiveForm ?? this);
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (rememberSession && !TutorialPreferences.Hidden)
                BeginInvoke(new Action(() => { if (!IsDisposed && Visible) StartTutorial(); }));
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
            string previous = currentView;
            currentView = slot;
            slot = previous;
            inlineRadar = currentView == "radar";
            if (currentView == "matrix")
            {
                diagramSize = new Size((int)(Width / AppliedWindowScale), (int)(Height / AppliedWindowScale));
                Height = Math.Max(Height, (int)(680 * AppliedWindowScale));
            }
            else if (previous == "matrix" && diagramSize.Width > 0)
                Size = new Size((int)(diagramSize.Width * AppliedWindowScale), (int)(diagramSize.Height * AppliedWindowScale));
            hovered = -1;
            affinityMask = 0;
            RefreshRadarShortcut();
            RefreshMatrixShortcut();
            canvas.Invalidate();
            SaveSession();
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
                using (var dialog = new MainForm())
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
                tips.SetToolTip(button, (button.Type < 0 ? "All elements" : Model.Names[button.Type]) + (button.Shortcut.Length > 0 ? " · " + button.Shortcut : ""));
                button.Invalidate();
            }
        }
        // Inputs: window-local key. Output: advertised digit or focused-row navigation. Fallback: native Tab/Space/Alt+F4. Raises: none.
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            int action = Array.IndexOf(bindings, keyData);
            if (action >= 10)
            {
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
            canvas.Invalidate();
            SaveSession();
        }
        private void LayoutRow()
        {
            row.Arrange(selection.Main >= 0);
        }

    }
}
