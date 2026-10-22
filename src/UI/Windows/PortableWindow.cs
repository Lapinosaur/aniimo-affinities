using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal partial class PortableWindow : Form
    {
        internal float AppliedWindowScale = 1;
        internal void ApplyWindowScale()
        {
            if (!customCaption || !(this is MainForm || this is RadarDialog || this is MatrixDialog))
                return;
            WindowZoom.Apply(this, DisplaySettings.WindowScale);
            RefreshWindowFrame();
            PositionUnlockWindow();
            if (unlockWindow != null)
                unlockWindow.RefreshSizes();
        }

        private bool customCaption;
        private readonly DrawingPanel windowFrame = new DrawingPanel { TabStop = false };
        // Inputs: current window mode and size. Output: a shared rounded frame; overlays stay frameless.
        private void RefreshWindowFrame()
        {
            if (!customCaption || windowFrame == null || ClientSize.Width < 2 || ClientSize.Height < 2)
                return;
            bool framed = !TopMost && WindowState != FormWindowState.Maximized;
            var previous = Region;
            if (framed)
                using (var outline = Drawing.Round(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), Theme.WindowRadius))
                    Region = new Region(outline);
            else
                Region = null;
            if (previous != null)
                previous.Dispose();
            Padding = framed ? new Padding(2) : Padding.Empty;
            windowFrame.Visible = framed;
            if (!framed)
                return;
            windowFrame.Bounds = ClientRectangle;
            using (var outside = Drawing.Round(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), Theme.WindowRadius))
            using (var inside = Drawing.Round(new RectangleF(2, 2, ClientSize.Width - 4, ClientSize.Height - 4), Theme.WindowRadius - 2))
            {
                var ring = new Region(outside);
                ring.Exclude(inside);
                var old = windowFrame.Region;
                windowFrame.Region = ring;
                if (old != null)
                    old.Dispose();
            }
            windowFrame.BringToFront();
            windowFrame.Invalidate();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RefreshWindowFrame();
        }
        private readonly Timer holdTimer = new Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch holdWatch = new System.Diagnostics.Stopwatch();
        private Button pinControl; private Panel captionPanel;
        protected Control TutorialCaptionTarget(string name)
        {
            return name == "pin" ? pinControl : name == "matrix" ? matrixControl : name == "radar" ? radarControl : null;
        }
        private bool suppressPinClick, unlockRegistered;
        private int unlockedStyle;
        private UnlockWindow unlockWindow;
        internal bool OverlayLocked
        {
            get; private set;
        }
        protected virtual void OnOverlayLockChanged()
        {
        }
        internal int VisualUpdateDepth;
        protected virtual bool ContentOnly
        {
            get
            {
                return false;
            }
        }
        protected virtual Action LockedPanelToggle
        {
            get
            {
                return null;
            }
        }
        protected virtual string LockedPanelGlyph
        {
            get
            {
                return "separate";
            }
        }
        private float holdProgress;
        private void CancelHold()
        {
            suppressPinClick |= holdWatch.ElapsedMilliseconds >= 300;
            holdTimer.Stop();
            holdWatch.Reset();
            holdProgress = 0;
            if (pinControl != null)
                pinControl.Invalidate();
        }
        protected void SetOverlayLocked(bool locked)
        {
            if (locked == OverlayLocked)
                return;
            if (locked)
            {
                var shortcut = KeyBindings.Load(KeyBindings.FilePath)[11];
                uint mods = 0x4000u | ((shortcut & Keys.Control) != 0 ? 2u : 0u) | ((shortcut & Keys.Alt) != 0 ? 1u : 0u) | ((shortcut & Keys.Shift) != 0 ? 4u : 0u);
                unlockRegistered = shortcut != Keys.None && NativeMethods.RegisterHotKey(Handle, 0x414E, mods, (uint)(shortcut & Keys.KeyCode));
                SetPinned(true);
                unlockedStyle = NativeMethods.GetWindowLong(Handle, -20);
                if (!NativeMethods.TrySetWindowStyle(Handle, unlockedStyle | 0x20 | 0x08000000))
                {
                    if (unlockRegistered)
                        NativeMethods.UnregisterHotKey(Handle, 0x414E);
                    unlockRegistered = false;
                    MessageBox.Show(this, "Windows could not lock the overlay. Please try again.", "Overlay");
                    return;
                }
                OverlayLocked = true;
            }
            else
            {
                if (!NativeMethods.TrySetWindowStyle(Handle, unlockedStyle))
                {
                    MessageBox.Show(this, "Windows could not unlock the overlay. Please try again.", "Overlay");
                    return;
                }
                OverlayLocked = false;
                if (unlockRegistered)
                    NativeMethods.UnregisterHotKey(Handle, 0x414E);
                unlockRegistered = false;
            }
            if (captionPanel != null)
                foreach (Control control in captionPanel.Controls)
                    control.Visible = !locked || control == pinControl;
            if (pinControl != null)
            {
                pinControl.AccessibleName = locked ? "Locked overlay · Ctrl+Alt+L to unlock" : "Always on top";
                pinControl.Invalidate();
            }
            if (locked && pinControl != null)
            {
                unlockWindow = new UnlockWindow(() => SetOverlayLocked(false), () => Close(), unlockRegistered, () => Location, point =>
                {
                    Location = KeepCaptionVisible(new Rectangle(point, Size), Screen.FromPoint(Cursor.Position).WorkingArea, 54).Location;
                }, LockedPanelToggle, () => LockedPanelGlyph);
                PositionUnlockWindow();
                unlockWindow.Show(this);
            }
            else if (unlockWindow != null)
            {
                var window = unlockWindow;
                unlockWindow = null;
                window.Dispose();
            }
            CancelHold();
            OnOverlayLockChanged();
            Invalidate(true);
        }
        private void PositionUnlockWindow()
        {
            if (unlockWindow != null && pinControl != null)
                unlockWindow.Bounds = new Rectangle(pinControl.PointToScreen(new Point((pinControl.Width - 32) / 2 - (unlockWindow.Width - 32), Math.Max(0, (pinControl.Height - 32) / 2))), new Size(unlockWindow.Width, 32));
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowRect
        {
            public int Left, Top, Right, Bottom;
        }
        // Inputs: proposed bounds and destination monitor work area. Output: reachable caption.
        // Horizontal overlap allows crossing monitors; vertical bounds keep the full caption visible.
        internal static Rectangle KeepCaptionVisible(Rectangle bounds, Rectangle workArea, int captionHeight)
        {
            int top = Math.Max(workArea.Top, Math.Min(bounds.Top, workArea.Bottom - captionHeight));
            int left = Math.Max(workArea.Left - bounds.Width + 48, Math.Min(bounds.Left, workArea.Right - 48));
            return new Rectangle(left, top, bounds.Width, bounds.Height);
        }
        private readonly ApplicationTips captionTips = ApplicationTips.Create();
        internal event Action<Rectangle> SupportBoundsChanged
        {
            add
            {
                captionTips.SupportBoundsChanged += value;
            }
            remove
            {
                captionTips.SupportBoundsChanged -= value;
            }
        }
        internal Func<Rectangle, Rectangle> SupportPlacement
        {
            set
            {
                captionTips.SupportPlacement = value;
            }
        }
        protected Control CaptionContent
        {
            get; set;
        }
        protected virtual void OpenSettings()
        {
        }
        protected virtual void OpenMatrix()
        {
        }
        protected virtual void OpenRadar()
        {
        }
        protected virtual bool InlineRadar
        {
            get
            {
                return false;
            }
        }
        protected virtual void ToggleInlineRadar()
        {
        }
        protected virtual void ToggleInlineMatrix()
        {
        }
        protected virtual string RadarViewGlyph
        {
            get
            {
                return "radar";
            }
        }
        protected virtual string MatrixViewGlyph
        {
            get
            {
                return "matrix";
            }
        }
        private static string ViewName(string view)
        {
            return view == "matrix" ? "Effectiveness matrix" : view == "radar" ? "Affinity radar" : "Affinity diagrams";
        }
        private static string ViewTip(string view, string key)
        {
            return ViewName(view) + "\nClick: swap this view with the main view. The button takes the previous view's icon.\nHold click or Space for 0.9 s: open in a separate window" + (key.Length > 0 ? " (or " + key + ")" : "") + ".";
        }
        private KeyboardHold matrixKeyboard, radarKeyboard, pinKeyboard;
        private Button viewHoldTarget;
        private bool holdingMatrix;
        private readonly Timer radarHoldTimer = new Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch radarHoldWatch = new System.Diagnostics.Stopwatch();
        private bool suppressRadarClick;
        private float radarHoldProgress;
        private void CancelRadarHold()
        {
            suppressRadarClick |= radarHoldWatch.ElapsedMilliseconds >= 225;
            radarHoldTimer.Stop();
            radarHoldWatch.Reset();
            radarHoldProgress = 0;
            if (radarControl != null)
                radarControl.Invalidate();
            if (matrixControl != null)
                matrixControl.Invalidate();
        }
        protected virtual Keys RadarShortcut
        {
            get
            {
                return Keys.None;
            }
        }
        private Button radarControl;
        protected void RefreshRadarShortcut()
        {
            if (radarControl == null)
                return;
            string key = KeyBindings.Label(RadarShortcut);
            captionTips.SetToolTip(radarControl, ViewTip(RadarViewGlyph, key));
            radarControl.AccessibleName = ViewName(RadarViewGlyph);
            radarControl.AccessibleDescription = key.Length > 0 ? "Shortcut: " + key : "No shortcut";
            radarControl.Invalidate();
        }
        protected virtual Keys MatrixShortcut
        {
            get
            {
                return Keys.None;
            }
        }
        private Button matrixControl;
        protected void RefreshMatrixShortcut()
        {
            if (matrixControl == null)
                return;
            string key = KeyBindings.Label(MatrixShortcut);
            captionTips.SetToolTip(matrixControl, ViewTip(MatrixViewGlyph, key));
            matrixControl.AccessibleName = ViewName(MatrixViewGlyph);
            matrixControl.AccessibleDescription = key.Length > 0 ? "Shortcut: " + key : "No shortcut";
            matrixControl.Invalidate();
        }
        protected virtual string CaptionGlyph
        {
            get
            {
                return "settings";
            }
        }
        protected virtual bool CompactCaption
        {
            get
            {
                return false;
            }
        }
        protected override bool ProcessCmdKey(ref Message message, Keys input)
        {
            if (!(this is DisplayConfirmationDialog) && WindowZoom.HandleShortcut(input, message.LParam))
                return true;
            if (input == Keys.Escape && CompactCaption)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref message, input);
        }
        // Inputs: passive surface. Output: native window dragging on left press.
        // Child controls retain their own mouse handlers. Fallback: ignore other buttons. Raises: none.
        protected void EnableWindowDrag(Control surface)
        {
            surface.Cursor = Cursors.SizeAll;
            surface.MouseDown += DragSurface;
        }
        private void DragSurface(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
        }
        // Inputs: pin state. Output: topmost overlay with transparent background, or normal opaque window.
        // Keep the dark key equal to the painted background so antialiased text has no colored fringe.
        internal void SetPinned(bool pinned)
        {
            TopMost = pinned;
            TransparencyKey = pinned ? Theme.Background : Color.Empty;
            RefreshWindowFrame();
            Invalidate(true);
        }
        private readonly Icon applicationIcon;
        internal PortableWindow()
        {
            radarHoldTimer.Tick += (s, e) =>
            {
                if (viewHoldTarget == null || !((holdingMatrix ? matrixKeyboard : radarKeyboard) != null && (holdingMatrix ? matrixKeyboard : radarKeyboard).Active) && (!viewHoldTarget.ClientRectangle.Contains(viewHoldTarget.PointToClient(Cursor.Position)) || (Control.MouseButtons & MouseButtons.Left) == 0))
                {
                    CancelRadarHold();
                    return;
                }
                radarHoldProgress = Math.Min(1f, radarHoldWatch.ElapsedMilliseconds / 900f);
                viewHoldTarget.Invalidate();
                if (radarHoldProgress >= 1)
                {
                    suppressRadarClick = true;
                    CancelRadarHold();
                    if (holdingMatrix)
                        OpenMatrix();
                    else
                        OpenRadar();
                }
            };
            Deactivate += (s, e) => CancelRadarHold();
            attentionTimer.Tick += (s, e) =>
            {
                double progress = attentionWatch.Elapsed.TotalMilliseconds / 320;
                if (progress >= 1 || !Visible)
                {
                    StopAttention();
                    return;
                }
                var offset = AttentionOffset(progress);
                Location = new Point(attentionOrigin.X + offset.X, attentionOrigin.Y + offset.Y);
            };
            FormClosing += (s, e) => StopAttention();
            windowFrame.Paint += (s, e) =>
            {
                e.Graphics.Clear(Theme.Background);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var outline = Drawing.Round(new RectangleF(0.5f, 0.5f, windowFrame.Width - 1, windowFrame.Height - 1), Theme.WindowRadius - 0.5f))
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawPath(pen, outline);
            };
            EnableWindowDrag(windowFrame);
            Deactivate += (s, e) => CancelHold();
            LocationChanged += (s, e) => PositionUnlockWindow();
            SizeChanged += (s, e) => PositionUnlockWindow();
            holdTimer.Tick += (s, e) =>
            {
                if (pinControl == null || !(pinKeyboard != null && pinKeyboard.Active) && (!pinControl.ClientRectangle.Contains(pinControl.PointToClient(Cursor.Position)) || (Control.MouseButtons & MouseButtons.Left) == 0))
                {
                    CancelHold();
                    return;
                }
                holdProgress = Math.Min(1f, holdWatch.ElapsedMilliseconds / 1200f);
                pinControl.Invalidate();
                if (holdProgress >= 1)
                {
                    suppressPinClick = true;
                    CancelHold();
                    NativeMethods.ReleaseCapture();
                    SetOverlayLocked(true);
                }
            };
            using (var stream = typeof(PortableWindow).Assembly.GetManifestResourceStream("app.ico"))
            using (var embedded = new Icon(stream))
                applicationIcon = (Icon)embedded.Clone();
            Icon = applicationIcon;
            ShowIcon = true;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                radarHoldTimer.Dispose();
                attentionTimer.Dispose();
                if (unlockWindow != null)
                    unlockWindow.Dispose();
                holdTimer.Dispose();
                applicationIcon.Dispose();
                captionTips.Dispose();
            }
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (unlockRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, 0x414E);
                unlockRegistered = false;
            }
            base.OnHandleDestroyed(e);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            using (new VisualUpdate(this))
            {
                if (IsDisposed || Disposing)
                    return;
                var caption = new Panel { Dock = DockStyle.Top, Height = CompactCaption ? 36 : 54, BackColor = Theme.Background };
                var commands = new Panel { Width = !CompactCaption && !ContentOnly ? 406 : 42, Height = caption.Height, Dock = DockStyle.Right, BackColor = Theme.Background };
                captionPanel = commands;
                caption.Controls.Add(commands);
                var title = new Label { Text = CaptionContent == null ? Text : "", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(40, 0, 0, 0), ForeColor = Theme.Muted };
                if (CompactCaption)
                    title.Paint += (s, a) =>
                    {
                        a.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        a.Graphics.ScaleTransform(AppliedWindowScale, AppliedWindowScale);
                        Glyphs.Draw(a.Graphics, CaptionGlyph, new RectangleF(14, (title.Height / AppliedWindowScale - 16) / 2f, 16, 16), Theme.Muted);
                    };
                var close = CaptionButton("×", "Close (Alt+F4)", true);
                if (CompactCaption)
                {
                    close.TabStop = false;
                    captionTips.SetToolTip(close, "Close · Esc");
                }
                var settings = CaptionButton("⚙", "Settings");
                var matrix = CaptionButton("matrix", "Effectiveness matrix");
                matrixControl = matrix;
                matrix.Click += (s, a) => { if (suppressRadarClick || radarHoldWatch.ElapsedMilliseconds >= 225) { return; } ToggleInlineMatrix(); };
                matrixKeyboard = new KeyboardHold(matrix, () => { viewHoldTarget = matrix; holdingMatrix = true; suppressRadarClick = false; radarHoldWatch.Restart(); radarHoldTimer.Start(); }, CancelRadarHold);
                matrix.MouseDown += (s, a) => { if (a.Button == MouseButtons.Left) { viewHoldTarget = matrix; holdingMatrix = true; suppressRadarClick = false; radarHoldWatch.Restart(); radarHoldTimer.Start(); } };
                matrix.MouseUp += (s, a) => CancelRadarHold();
                matrix.MouseLeave += (s, a) => { if (!matrixKeyboard.Active) CancelRadarHold(); };
                matrix.MouseCaptureChanged += (s, a) => { if (!matrix.Capture && !matrixKeyboard.Active) CancelRadarHold(); };
                RefreshMatrixShortcut();
                var radar = CaptionButton("radar", "Affinity radar");
                radarControl = radar;
                radar.Click += (s, a) => { if (suppressRadarClick || radarHoldWatch.ElapsedMilliseconds >= 225) { return; } ToggleInlineRadar(); };
                RefreshRadarShortcut();
                radarKeyboard = new KeyboardHold(radar, () => { viewHoldTarget = radar; holdingMatrix = false; suppressRadarClick = false; radarHoldWatch.Restart(); radarHoldTimer.Start(); }, CancelRadarHold);
                radar.MouseDown += (s, a) => { if (a.Button == MouseButtons.Left) { viewHoldTarget = radar; holdingMatrix = false; suppressRadarClick = false; radarHoldWatch.Restart(); radarHoldTimer.Start(); } };
                radar.MouseUp += (s, a) => CancelRadarHold();
                radar.MouseLeave += (s, a) => { if (!radarKeyboard.Active) CancelRadarHold(); };
                radar.MouseCaptureChanged += (s, a) => { if (!radar.Capture && !radarKeyboard.Active) CancelRadarHold(); };
                UiFonts.Assign(settings, UiFonts.Create("Segoe UI Symbol", 13));
                captionTips.SetToolTip(settings, "Settings · Keyboard shortcuts");
                settings.Click += (s, a) => OpenSettings();
                EnableWindowDrag(title);
                EnableWindowDrag(caption);
                var coffee = new SupportButton { Dock = DockStyle.Right, Width = 154 };
                UiFonts.Assign(coffee, UiFonts.Create("Segoe UI", 9));
                coffee.Cursor = Cursors.Hand;
                captionTips.SetToolTip(coffee, "Support Lapinosaur\nScan with your phone");
                captionTips.SetAnchorBounds(coffee, () =>
                {
                    int height = (int)Math.Round(32 * AppliedWindowScale);
                    return new Rectangle(0, (coffee.Height - height) / 2, coffee.Width, height);
                });
                coffee.Click += (s, a) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://buymeacoffee.com/lapinosaur") { UseShellExecute = true });
                    }
                    catch (System.ComponentModel.Win32Exception) { MessageBox.Show(this, "https://buymeacoffee.com/lapinosaur", "Open in your browser", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
                var pin = CaptionButton("", "Always on top", false, true);
                pinControl = pin;
                pinKeyboard = new KeyboardHold(pin, () => { suppressPinClick = false; holdWatch.Restart(); holdTimer.Start(); }, CancelHold);
                pin.MouseDown += (s, a) => { if (a.Button == MouseButtons.Left) { suppressPinClick = false; holdWatch.Restart(); holdTimer.Start(); } };
                pin.MouseUp += (s, a) => CancelHold();
                pin.MouseLeave += (s, a) => { if (!pinKeyboard.Active) CancelHold(); };
                pin.MouseCaptureChanged += (s, a) => { if (!pin.Capture && !pinKeyboard.Active) CancelHold(); };
                pin.Click += (s, a) => { if (suppressPinClick || holdWatch.ElapsedMilliseconds >= 300 || OverlayLocked) return; SetPinned(!TopMost); pin.AccessibleName = TopMost ? "Disable always on top" : "Always on top"; pin.Invalidate(); };
                captionTips.SetToolTip(pin, "Click to pin · Hold to lock overlay");
                var minimize = CaptionButton("−", "Minimize");
                captionTips.SetToolTip(close, CompactCaption ? "Close · Esc / Alt+F4" : "Close · Alt+F4");
                captionTips.SetToolTip(minimize, "Minimize");
                close.Click += (s, a) => Close();
                minimize.Click += (s, a) => WindowState = FormWindowState.Minimized;
                title.DoubleClick += (s, a) => { if (!CompactCaption) WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; };
                caption.Controls.Add(title);
                if (CaptionContent != null)
                {
                    CaptionContent.Dock = DockStyle.Left;
                    caption.Controls.Add(CaptionContent);
                }
                if (!CompactCaption && !ContentOnly)
                {
                    commands.Controls.Add(matrix);
                    commands.Controls.Add(radar);
                    commands.Controls.Add(pin);
                    commands.Controls.Add(coffee);
                    commands.Controls.Add(settings);
                    commands.Controls.Add(minimize);
                }
                else
                {
                    matrix.Dispose();
                    radar.Dispose();
                    settings.Dispose();
                    coffee.Dispose();
                    pin.Dispose();
                    minimize.Dispose();
                }
                commands.Controls.Add(close);
                commands.SendToBack();
                Controls.Add(caption);
                caption.BringToFront();
                // Docking order: title above the existing selector, with the canvas filling the rest.
                Controls.SetChildIndex(caption, Controls.Count - 1);
                caption.CreateControl();
                close.CreateControl();
                if (IsDisposed || Disposing)
                    return;
                var size = ClientSize;
                FormBorderStyle = FormBorderStyle.None;
                ClientSize = size;
                customCaption = true;
                windowFrame.PhysicalPainting = true;
                Controls.Add(windowFrame);
                RefreshWindowFrame();
                ApplyWindowScale();
            }
        }
        // Inputs: caption action. Output: consistent rounded surface, hover and keyboard focus.
        // Artwork-only actions keep their supplied branding. Raises: none.

    }
}
