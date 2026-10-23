using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class KeyCapture : Control, IMessageFilter
    {
        private Keys binding;
        private bool listening, hover;
        internal bool Listening
        {
            get
            {
                return listening;
            }
        }
        internal bool TestHeld;
        private string rejectedKey;
        private Keys pending, heldModifiers, primaryKey;
        private bool primaryReleased;
        private void BeginCapture()
        {
            if (!Enabled || !Visible)
                return;
            ShowValidation(false, false);
            pending = Keys.None;
            heldModifiers = Keys.None;
            primaryReleased = false;
            listening = true;
            rejectedKey = null;
            if (Feedback != null)
                Feedback("Press a key, then release it. Esc cancels.");
            Invalidate();
        }
        internal void ReleaseCapturedKey(Keys key)
        {
            if (key == primaryKey)
                primaryReleased = true;
            if (key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey)
                heldModifiers &= ~Keys.Control;
            if (key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey)
                heldModifiers &= ~Keys.Shift;
            if (key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu)
                heldModifiers &= ~Keys.Alt;
            Invalidate();
            if (pending != Keys.None && primaryReleased && heldModifiers == Keys.None)
            {
                Keys result = pending;
                pending = Keys.None;
                Binding = result;
                ShowValidation(false, true);
                if (Feedback != null)
                    Feedback("Shortcut updated. Click Save to keep your changes.");
            }
        }
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg == 0x1C && message.WParam == IntPtr.Zero && listening)
            {
                RefreshLabel();
                return false;
            }
            if (!listening || !ContainsFocus)
                return false;
            if (message.Msg == 0x101 || message.Msg == 0x105)
            {
                ReleaseCapturedKey((Keys)message.WParam.ToInt32());
                return true;
            }
            return false;
        }
        internal Action<string> Feedback;
        internal bool AllowModifiers;
        internal event EventHandler BindingChanged, CaptureEnded;
        private readonly Timer validationTimer = new Timer { Interval = 1500 };
        internal Func<Keys, bool> ValidateBinding;
        internal bool Invalid, CaptureValidated;
        internal void ShowValidation(bool invalid, bool validated)
        {
            validationTimer.Stop();
            Invalid = invalid;
            CaptureValidated = validated;
            if (validated)
                validationTimer.Start();
            Invalidate();
        }
        internal Keys Binding
        {
            get
            {
                return binding;
            }
            set
            {
                binding = value;
                RefreshLabel();
                if (CaptureEnded != null)
                    CaptureEnded(this, EventArgs.Empty);
                if (BindingChanged != null)
                    BindingChanged(this, EventArgs.Empty);
            }
        }
        internal KeyCapture()
        {
            Application.AddMessageFilter(this);
            validationTimer.Tick += (s, e) => ShowValidation(false, false);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            TabStop = true;
            Height = 28;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleDescription = "Press Enter or Space to change this shortcut. Release the new key to confirm. Escape cancels; Delete clears.";
        }
        private void RefreshLabel()
        {
            pending = Keys.None;
            heldModifiers = Keys.None;
            listening = false;
            Invalid = false;
            CaptureValidated = false;
            rejectedKey = null;
            Text = binding == Keys.None ? "Unassigned" : KeyBindings.Label(binding);
            Invalidate();
        }
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Invalidate();
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled)
            {
                RefreshLabel();
                TestHeld = false;
            }
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible)
                RefreshLabel();
        }
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new CaptureAccess(this);
        }
        private sealed class CaptureAccess : ControlAccessibleObject
        {
            private readonly KeyCapture capture;
            internal CaptureAccess(KeyCapture capture) : base(capture) { this.capture = capture; }
            public override string Value
            {
                get
                {
                    return capture.listening ? "Waiting for shortcut" : capture.Text;
                }
            }
            public override string DefaultAction
            {
                get
                {
                    return "Change shortcut";
                }
            }
            public override void DoDefaultAction()
            {
                if (capture.Enabled && capture.Visible)
                {
                    capture.Focus();
                    capture.BeginCapture();
                }
            }
        }
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            RefreshLabel();
            if (CaptureEnded != null)
                CaptureEnded(this, EventArgs.Empty);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.RemoveMessageFilter(this);
                validationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                BeginCapture();
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = false;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(TestHeld ? Theme.AccentSurface : Theme.Background);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Drawing.Box(g, new RectangleF(2, 2, Width - 5, Height - 5), listening || TestHeld ? Theme.AccentSurface : Theme.Surface, listening || TestHeld ? Theme.AccentForeground : Focused ? Theme.AccentForeground : hover ? Theme.StrongBorder : Theme.Border, 6);
            string modifiers = (heldModifiers.HasFlag(Keys.Control) ? "Ctrl + " : "") + (heldModifiers.HasFlag(Keys.Shift) ? "Shift + " : "") + (heldModifiers.HasFlag(Keys.Alt) ? "Alt + " : "");
            Drawing.Text(g, Invalid && rejectedKey != null ? rejectedKey + " · Not assigned" : pending != Keys.None ? KeyBindings.Label(pending) + " · Release" : listening ? modifiers + "Press a key…" : Text, Font, Invalid ? Theme.Error : listening || TestHeld ? Theme.AccentForeground : binding == Keys.None ? Theme.Muted : Theme.TextColor, new RectangleF(8, 0, Width - (Invalid || CaptureValidated ? 34 : 16), Height), true);
            if (Invalid || CaptureValidated)
            {
                Color color = Invalid ? Theme.Error : Theme.AccentForeground;
                using (var outline = Drawing.Round(new RectangleF(2, 2, Width - 5, Height - 5), 6))
                using (var pen = new Pen(color, 1.5f))
                    g.DrawPath(pen, outline);
                Drawing.Text(g, Invalid ? "!" : "✓", Font, color, new RectangleF(Width - 24, 0, 20, Height), true);
            }
            if (!Enabled)
                using (var shade = new SolidBrush(Color.FromArgb(110, Theme.Surface)))
                using (var outline = Drawing.Round(new RectangleF(2, 2, Width - 5, Height - 5), 6))
                    g.FillPath(shade, outline);
        }
        // Inputs: focused field keystroke. Output: one binding or Delete unbind. Fallback: Tab/Escape retain navigation/cancel. Raises: none.
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (!Enabled)
                return base.ProcessCmdKey(ref message, keyData);
            Keys key = keyData & Keys.KeyCode, modifiers = keyData & Keys.Modifiers;
            if (key == Keys.Escape && listening)
            {
                RefreshLabel();
                if (CaptureEnded != null)
                    CaptureEnded(this, EventArgs.Empty);
                return true;
            }
            if (pending != Keys.None)
                return true;
            if (key == Keys.Tab || key == Keys.Escape)
                return base.ProcessCmdKey(ref message, keyData);
            if ((message.LParam.ToInt64() & (1L << 30)) != 0)
                return true;
            if ((key == Keys.Space || key == Keys.Enter) && modifiers == Keys.None && !listening)
            {
                BeginCapture();
                return true;
            }
            if (key == Keys.Delete && modifiers == Keys.None)
            {
                Binding = Keys.None;
                if (Feedback != null)
                    Feedback("");
                return true;
            }
            if (!listening)
                return base.ProcessCmdKey(ref message, keyData);
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu)
            {
                heldModifiers = modifiers;
                Invalidate();
                return true;
            }
            if (key == Keys.F4 && modifiers == Keys.Alt)
                return base.ProcessCmdKey(ref message, keyData);
            Keys physicalKey = key;
            key = KeyBindings.NormalizeNumber(key);
            if ((AllowModifiers || modifiers == Keys.None || modifiers == Keys.Shift && key >= Keys.D0 && key <= Keys.D9) && KeyBindings.IsChoice(key))
            {
                if (AllowModifiers)
                    key |= modifiers;
                if (ValidateBinding != null && !ValidateBinding(key))
                {
                    if (!listening)
                        return true;
                    rejectedKey = KeyBindings.Label(key);
                    ShowValidation(true, false);
                    return true;
                }
                pending = key;
                ShowValidation(false, false);
                rejectedKey = null;
                primaryKey = physicalKey;
                heldModifiers = modifiers;
                primaryReleased = false;
                Invalidate();
            }
            else
            {
                rejectedKey = KeyBindings.Label(keyData);
                ShowValidation(true, false);
                if (Feedback != null)
                    Feedback(AllowModifiers ? "Use a letter, number or function key, optionally with Ctrl, Alt or Shift." : "Use a single letter, number or function key for this action.");
            }
            return true;
        }
    }
}
