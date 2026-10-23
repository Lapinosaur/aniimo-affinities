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
        private readonly Dictionary<Control, Action> openActions = new Dictionary<Control, Action>();
        private readonly Dictionary<Control, Action> applyActions = new Dictionary<Control, Action>();
        internal void SetApplyAction(Control control, Action action)
        {
            if (action == null)
                applyActions.Remove(control);
            else
                applyActions[control] = action;
        }
        private readonly Dictionary<Control, Action> previewActions = new Dictionary<Control, Action>();
        internal void SetPreviewAction(Control control, Action action)
        {
            if (action == null)
                previewActions.Remove(control);
            else
                previewActions[control] = action;
        }
        private readonly HashSet<Control> hoverBubbles = new HashSet<Control>();
        internal void SetHoverBubble(Control control, bool enabled)
        {
            if (enabled)
                hoverBubbles.Add(control);
            else
                hoverBubbles.Remove(control);
        }
        private readonly HashSet<Control> scrollableGraphics = new HashSet<Control>();
        internal void SetScrollableGraphic(Control control, bool enabled)
        {
            if (enabled)
                scrollableGraphics.Add(control);
            else
                scrollableGraphics.Remove(control);
        }
        private readonly System.Diagnostics.Stopwatch outside = new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch lifetime = new System.Diagnostics.Stopwatch();
        internal void SetOpenAction(Control control, Action action)
        {
            if (action == null)
                openActions.Remove(control);
            else
                openActions[control] = action;
        }
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
            Func<Rectangle> previousBounds;
            if (displayed && ReferenceEquals(anchor, control) && anchors.TryGetValue(control, out previousBounds) && previousBounds() != bounds)
                Hide();
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
                control.AccessibleDescription = System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(text ?? "", @"\[types:[^\]]*\]", ""), @"\[key:([^\]]*)\]", "$1");
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
            openActions.Remove(control);
            applyActions.Remove(control);
            previewActions.Remove(control);
            hoverBubbles.Remove(control);
            scrollableGraphics.Remove(control);
            anchors.Remove(control);
            graphics.Remove(control);
            graphicSizes.Remove(control);
            if (ReferenceEquals(anchor, control))
                Hide();
        }
        private Control HoverRoot()
        {
            var root = anchor;
            while (root.Parent != null && hoverBubbles.Contains(root.Parent))
                root = root.Parent;
            return root;
        }
        internal static bool InHoverArea(Rectangle source, Rectangle popup, Point point)
        {
            if (source.Contains(point) || popup.Contains(point))
                return true;
            // Keep the short bridge between the two surfaces interactive, regardless of crossing speed.
            if (popup.Bottom <= source.Top)
                return Rectangle.FromLTRB(Math.Min(source.Left, popup.Left), popup.Bottom, Math.Max(source.Right, popup.Right), source.Top + 1).Contains(point);
            if (source.Bottom <= popup.Top)
                return Rectangle.FromLTRB(Math.Min(source.Left, popup.Left), source.Bottom, Math.Max(source.Right, popup.Right), popup.Top + 1).Contains(point);
            return false;
        }
        private void Enter(object sender, EventArgs e)
        {
            if (displayed && anchor != null && hoverBubbles.Contains(anchor))
            {
                var root = HoverRoot();
                var next = sender as Control;
                if (ReferenceEquals(root, next) || next != null && root.Contains(next))
                    return;
            }
            Hide();
            anchor = (Control)sender;
            timer.Interval = anchor is SupportButton ? 900 : 450;
            timer.Start();
        }
        private void Leave(object sender, EventArgs e)
        {
            // Actionable bubbles remain reachable after leaving their anchor.
            if (displayed && sender is Control && !(sender is Form) && anchor != null && hoverBubbles.Contains(anchor) && anchor.Enabled && anchor.Visible)
                return;
            Hide();
        }
        private void Down(object sender, MouseEventArgs e)
        {
            Hide();
        }
        private void Tick(object sender, EventArgs e)
        {
            if (displayed)
            {
                if (anchor != null && hoverBubbles.Contains(anchor))
                {
                    if (anchor.IsDisposed || !anchor.Visible || !anchor.Enabled)
                    {
                        Hide();
                        return;
                    }
                    var root = HoverRoot();
                    Func<Rectangle> region;
                    var area = root.RectangleToScreen(root == anchor && anchors.TryGetValue(anchor, out region) ? region() : root.ClientRectangle);
                    if (InHoverArea(area, bubble.Bounds, Cursor.Position))
                        outside.Reset();
                    else if (!outside.IsRunning)
                        outside.Start();
                    else if (outside.ElapsedMilliseconds >= 250)
                        Hide();
                }
                else
                {
                    bubble.Remaining = Math.Max(0, 1f - (float)lifetime.Elapsed.TotalMilliseconds / 8000);
                    bubble.Invalidate();
                    if (bubble.Remaining <= 0)
                        Hide();
                }
                return;
            }
            PresentTip(true);
        }
        internal static Form OwnerForm(Control control)
        {
            return control.TopLevelControl as Form ?? control.FindForm();
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
            owner = OwnerForm(anchor);
            // Embedded forms and modal owners can report a different ActiveForm.
            // Use the actual input target, also rejecting controls covered by another window.
            var hovered = requireActive ? Control.FromHandle(NativeMethods.WindowFromPoint(Cursor.Position)) : null;
            bool receivesInput = anchor.ContainsFocus || hovered == anchor || hovered != null && anchor.Contains(hovered);
            if (owner == null || requireActive && !receivesInput)
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
            Action open;
            openActions.TryGetValue(anchor, out open);
            bubble.SetOpenAction(open == null ? (Action)null : () => { Hide(); open(); });
            Action apply;
            applyActions.TryGetValue(anchor, out apply);
            bubble.SetApplyAction(apply == null ? (Action)null : () => { Hide(); apply(); });
            Action preview;
            previewActions.TryGetValue(anchor, out preview);
            bubble.SetPreviewAction(preview == null ? (Action)null : () => { Hide(); preview(); });
            bubble.SetDismissAction(hoverBubbles.Contains(anchor) ? (Action)Hide : null);
            bubble.Remaining = support == null && !hoverBubbles.Contains(anchor) ? 1 : 0;
            bubble.ScrollGraphic = scrollableGraphics.Contains(anchor);
            bubble.Present(anchor, text, anchors.TryGetValue(anchor, out bounds) ? bounds() : anchor.ClientRectangle, support == null ? null : support.QrCode, paint, paint == null ? (Size?)null : graphicSizes[anchor]);
            displayed = true;
            if (support != null && SupportPlacement != null)
                bubble.Relocate(SupportPlacement(bubble.Bounds));
            if (support != null && SupportBoundsChanged != null)
                SupportBoundsChanged(bubble.Bounds);
            if (support == null)
            {
                lifetime.Restart();
                timer.Interval = 50;
                timer.Start();
            }
        }
        private void Hide()
        {
            if (displayed && anchor is SupportButton && SupportBoundsChanged != null)
                SupportBoundsChanged(Rectangle.Empty);
            timer.Stop();
            lifetime.Reset();
            outside.Reset();
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
            openActions.Clear();
            applyActions.Clear();
            previewActions.Clear();
            hoverBubbles.Clear();
            graphicSizes.Clear();
        }
    }
}
