using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class ApplicationTips : IDisposable
    {
        private readonly Dictionary<Control, string> labels = new Dictionary<Control, string>();
        private readonly Dictionary<Control, Func<Rectangle>> anchors = new Dictionary<Control, Func<Rectangle>>();
        internal void SetAnchorBounds(Control control, Func<Rectangle> bounds)
        {
            anchors[control] = bounds;
        }
        private readonly Timer timer = new Timer();
        private readonly TipBubble bubble = new TipBubble();
        internal event Action<Rectangle> SupportBoundsChanged;
        internal Func<Rectangle, Rectangle> SupportPlacement;
        private Control anchor; private Form owner; private bool displayed; private bool disposed;
        internal static ApplicationTips Create()
        {
            return new ApplicationTips();
        }
        private ApplicationTips()
        {
            timer.Interval = 450;
            timer.Tick += Tick;
        }
        internal void SetToolTip(Control control, string text)
        {
            string previous;
            bool refresh = ReferenceEquals(anchor, control) && labels.TryGetValue(control, out previous) && previous != text;
            if (!labels.ContainsKey(control))
            {
                control.MouseEnter += Enter;
                control.MouseLeave += Leave;
                control.MouseDown += Down;
                control.GotFocus += Enter;
                control.LostFocus += Leave;
                control.EnabledChanged += Leave;
                control.Disposed += RemoveControl;
            }
            labels[control] = text;
            if (string.IsNullOrEmpty(control.AccessibleDescription))
                control.AccessibleDescription = System.Text.RegularExpressions.Regex.Replace(text ?? "", @"\[types:[^\]]*\]", "");
            if (refresh)
                Enter(control, EventArgs.Empty);
        }
        private void RemoveControl(object sender, EventArgs e)
        {
            var control = (Control)sender;
            control.MouseEnter -= Enter;
            control.MouseLeave -= Leave;
            control.MouseDown -= Down;
            control.GotFocus -= Enter;
            control.LostFocus -= Leave;
            control.EnabledChanged -= Leave;
            control.Disposed -= RemoveControl;
            labels.Remove(control);
            anchors.Remove(control);
            if (ReferenceEquals(anchor, control))
                Hide();
        }
        private void Enter(object sender, EventArgs e)
        {
            Hide();
            anchor = (Control)sender;
            timer.Interval = anchor is SupportButton ? 900 : 450;
            timer.Start();
        }
        private void Leave(object sender, EventArgs e)
        {
            Hide();
        }
        private void Down(object sender, MouseEventArgs e)
        {
            Hide();
        }
        private void Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (displayed)
            {
                Hide();
                return;
            }
            if (anchor == null || anchor.IsDisposed || !anchor.Visible || !anchor.Enabled)
                return;
            owner = anchor.FindForm();
            if (owner == null || Form.ActiveForm != owner)
                return;
            string text;
            if (!labels.TryGetValue(anchor, out text) || string.IsNullOrWhiteSpace(text))
                return;
            owner.Deactivate += Leave;
            owner.LocationChanged += Leave;
            Func<Rectangle> bounds;
            var support = anchor as SupportButton;
            bubble.Present(anchor, text, anchors.TryGetValue(anchor, out bounds) ? bounds() : anchor.ClientRectangle, support == null ? null : support.QrCode);
            displayed = true;
            if (support != null && SupportPlacement != null)
                bubble.Relocate(SupportPlacement(bubble.Bounds));
            if (support != null && SupportBoundsChanged != null)
                SupportBoundsChanged(bubble.Bounds);
            if (support == null)
            {
                timer.Interval = 8000;
                timer.Start();
            }
        }
        private void Hide()
        {
            if (displayed && anchor is SupportButton && SupportBoundsChanged != null)
                SupportBoundsChanged(Rectangle.Empty);
            timer.Stop();
            bubble.Hide();
            displayed = false;
            anchor = null;
            if (owner != null)
            {
                owner.Deactivate -= Leave;
                owner.LocationChanged -= Leave;
                owner = null;
            }
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Hide();
            timer.Dispose();
            bubble.Dispose();
            foreach (var item in labels)
            {
                item.Key.MouseEnter -= Enter;
                item.Key.MouseLeave -= Leave;
                item.Key.MouseDown -= Down;
                item.Key.GotFocus -= Enter;
                item.Key.LostFocus -= Leave;
                item.Key.EnabledChanged -= Leave;
                item.Key.Disposed -= RemoveControl;
            }
            labels.Clear();
            anchors.Clear();
        }
    }
}
