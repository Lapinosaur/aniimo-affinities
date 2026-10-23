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
        private readonly Dictionary<Control, Action<Graphics, RectangleF>> graphics = new Dictionary<Control, Action<Graphics, RectangleF>>();
        private readonly Dictionary<Control, Size> graphicSizes = new Dictionary<Control, Size>();
        internal void SetGraphicTip(Control control, string description, Size size, Action<Graphics, RectangleF> paint)
        {
            SetToolTip(control, description);
            graphics[control] = paint;
            graphicSizes[control] = size;
        }
        internal void SetAnchorBounds(Control control, Func<Rectangle> bounds)
        {
            anchors[control] = bounds;
        }
        internal void SetRegionTip(Control control, string text, Rectangle bounds, Action<Graphics, RectangleF> paint = null, Size? size = null)
        {
            SetToolTip(control, text);
            if (paint != null && size.HasValue)
            {
                graphics[control] = paint;
                graphicSizes[control] = size.Value;
            }
            else
            {
                graphics.Remove(control);
                graphicSizes.Remove(control);
            }
            SetAnchorBounds(control, () => bounds);
            if (string.IsNullOrWhiteSpace(text))
                Hide();
            else
                Enter(control, EventArgs.Empty);
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
        internal void ShowTip(Control control)
        {
            if (disposed || control.IsDisposed || !control.Enabled || !control.Visible)
                return;
            Enter(control, EventArgs.Empty);
            PresentTip(false);
        }
        private ApplicationTips()
        {
            timer.Interval = 450;
            timer.Tick += Tick;
        }
        internal void SetToolTip(Control control, string text)
        {
            graphics.Remove(control);
            graphicSizes.Remove(control);
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
            graphics.Remove(control);
            graphicSizes.Remove(control);
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
            PresentTip(true);
        }
        private void PresentTip(bool requireActive)
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
            if (owner == null || requireActive && Form.ActiveForm != owner)
                return;
            string text;
            if (!labels.TryGetValue(anchor, out text) || string.IsNullOrWhiteSpace(text))
                return;
            owner.Deactivate += Leave;
            owner.LocationChanged += Leave;
            Func<Rectangle> bounds;
            var support = anchor as SupportButton;
            Action<Graphics, RectangleF> paint;
            graphics.TryGetValue(anchor, out paint);
            bubble.Present(anchor, text, anchors.TryGetValue(anchor, out bounds) ? bounds() : anchor.ClientRectangle, support == null ? null : support.QrCode, paint, paint == null ? (Size?)null : graphicSizes[anchor]);
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
            graphics.Clear();
            graphicSizes.Clear();
        }
    }
}
