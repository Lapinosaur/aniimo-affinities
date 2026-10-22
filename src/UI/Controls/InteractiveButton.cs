using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    // Native Button owns activation and tab navigation; this base owns shared visual state.
    internal class InteractiveButton : Button
    {
        protected bool InteractionPressed
        {
            get; private set;
        }
        protected virtual bool IsBusy
        {
            get
            {
                return false;
            }
        }
        protected virtual bool IsSelected
        {
            get
            {
                return false;
            }
        }
        internal InteractiveButton()
        {
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            InteractionPressed = false;
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnChangeUICues(UICuesEventArgs e)
        {
            base.OnChangeUICues(e);
            Invalidate();
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            InteractionPressed = false;
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            base.OnEnabledChanged(e);
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            InteractionPressed = Enabled && e.Button == MouseButtons.Left;
            base.OnMouseDown(e);
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            InteractionPressed = false;
            base.OnMouseUp(e);
            Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (!Capture)
                InteractionPressed = false;
            base.OnMouseCaptureChanged(e);
            Invalidate();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && e.KeyCode == Keys.Space)
                InteractionPressed = true;
            base.OnKeyDown(e);
            Invalidate();
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            InteractionPressed = false;
            base.OnKeyUp(e);
            Invalidate();
        }
        protected override void OnClick(EventArgs e)
        {
            if (Enabled)
                base.OnClick(e);
        }
        internal void FinishPaint(Graphics graphics, float width, float height, bool dimDisabled = true)
        {
            graphics.ResetClip();
            if (!Enabled && dimDisabled)
                using (var shade = new SolidBrush(Color.FromArgb(110, Theme.Surface)))
                using (var outline = Drawing.Round(new RectangleF(1, 1, width - 3, height - 3), Theme.ControlRadius))
                    graphics.FillPath(shade, outline);
            if (Enabled && Focused && ShowFocusCues && width > 8 && height > 8)
                using (var path = Drawing.Round(new RectangleF(3, 3, width - 7, height - 7), 5))
                using (var pen = new Pen(Theme.AccentForeground, 1.5f))
                    graphics.DrawPath(pen, path);
        }
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new ButtonAccess(this);
        }
        private sealed class ButtonAccess : ControlAccessibleObject
        {
            private readonly InteractiveButton button;
            internal ButtonAccess(InteractiveButton button) : base(button) { this.button = button; }
            public override string DefaultAction
            {
                get
                {
                    return button is HoldSizeButton ? "Hold Space" : "Press";
                }
            }
            public override void DoDefaultAction()
            {
                if (button.Enabled && button.Visible)
                    button.PerformClick();
            }
            public override AccessibleStates State
            {
                get
                {
                    return base.State | (button.IsSelected ? AccessibleStates.Selected : AccessibleStates.None) | (button.IsBusy ? AccessibleStates.Busy : AccessibleStates.None);
                }
            }
        }
    }
}
