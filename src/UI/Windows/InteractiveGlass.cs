using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace AniimoAffinities
{
    // The real form retains mouse, keyboard, focus and dragging. Click-through
    // visual companions sit directly behind it, in its normal desktop z-order.
    internal sealed class InteractiveGlass : NativeWindow, IDisposable
    {
        private readonly Form source;
        private readonly LockedGlass.GlassWindow foreground = new LockedGlass.GlassWindow(false);
        private readonly LockedGlass.GlassWindow backdrop = new LockedGlass.GlassWindow(false);
        private readonly HashSet<Control> watched = new HashSet<Control>();
        private bool queued, rendering, disposed, failed, active, arranging;
        private int revision = -1;
        private Rectangle backdropArea;
        private readonly IntPtr[] positionHandles = new IntPtr[2];
        private readonly Rectangle[] positionBounds = new Rectangle[2];
        internal bool Active
        {
            get
            {
                return active;
            }
        }
        internal InteractiveGlass(Form form)
        {
            source = form;
            AssignHandle(form.Handle);
            Watch(form);
            Theme.Changed += Queue;
            Render();
        }
        private void Watch(Control control)
        {
            if (!watched.Add(control))
                return;
            control.Invalidated += Invalidated;
            control.SizeChanged += Changed;
            control.VisibleChanged += Changed;
            control.TextChanged += Changed;
            control.ControlAdded += Added;
            control.ControlRemoved += Removed;
            foreach (Control child in control.Controls)
                Watch(child);
        }
        private void Unwatch(Control control)
        {
            if (!watched.Remove(control))
                return;
            control.Invalidated -= Invalidated;
            control.SizeChanged -= Changed;
            control.VisibleChanged -= Changed;
            control.TextChanged -= Changed;
            control.ControlAdded -= Added;
            control.ControlRemoved -= Removed;
            foreach (Control child in control.Controls)
                Unwatch(child);
        }
        private void Added(object sender, ControlEventArgs e)
        {
            Watch(e.Control);
            Queue();
        }
        private void Removed(object sender, ControlEventArgs e)
        {
            Unwatch(e.Control);
            Queue();
        }
        private void Changed(object sender, EventArgs e)
        {
            Queue();
        }
        private void Invalidated(object sender, InvalidateEventArgs e)
        {
            Queue();
        }
        private void Queue()
        {
            if (queued || rendering || disposed || failed || !source.IsHandleCreated)
                return;
            queued = true;
            source.BeginInvoke(new Action(() => { queued = false; Render(); }));
        }
        private void Position()
        {
            if (disposed || arranging || !active)
                return;
            arranging = true;
            try
            {
                if (!source.Visible || source.WindowState == FormWindowState.Minimized)
                {
                    foreground.Hide();
                    backdrop.Hide();
                    return;
                }
                var position = source.PointToScreen(Point.Empty);
                foreground.PrepareBounds(new Rectangle(position, source.ClientSize));
                var blur = backdropArea;
                blur.Offset(position);
                backdrop.PrepareBounds(blur);
                positionHandles[0] = foreground.Handle;
                positionHandles[1] = backdrop.Handle;
                positionBounds[0] = foreground.Bounds;
                positionBounds[1] = backdrop.Bounds;
                GlassNative.PositionTogether(positionHandles, positionBounds, true);
                foreground.MarkVisible();
                backdrop.MarkVisible();
                GlassNative.Behind(foreground.Handle, source.Handle);
                GlassNative.Behind(backdrop.Handle, foreground.Handle);
            }
            finally { arranging = false; }
        }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == 0x47)
                Position();
        }
        private void Render()
        {
            if (disposed || rendering || failed || source.IsDisposed)
                return;
            if (!source.Visible || source.WindowState == FormWindowState.Minimized)
            {
                Position();
                return;
            }
            if (SystemInformation.HighContrast)
            {
                Restore();
                failed = true;
                return;
            }
            rendering = true;
            try
            {
                if (revision != Theme.Revision && !GlassNative.Enable(backdrop.Handle, Theme.Surface))
                {
                    Restore();
                    failed = true;
                    return;
                }
                using (var image = new Bitmap(Math.Max(1, source.ClientSize.Width), Math.Max(1, source.ClientSize.Height)))
                {
                    source.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    using (var silhouette = GlassFrame.Prepare(image, true))
                    {
                    }
                    if (source.Region != null)
                        using (var graphics = Graphics.FromImage(image))
                        using (var outside = new Region(new Rectangle(Point.Empty, image.Size)))
                        {
                            outside.Exclude(source.Region);
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.FillRegion(Brushes.Transparent, outside);
                        }
                    if (!GlassNative.Present(foreground.Handle, image, source.PointToScreen(Point.Empty)))
                    {
                        Restore();
                        failed = true;
                        return;
                    }
                }
                using (var silhouette = source.Region == null ? new Region(source.ClientRectangle) : source.Region.Clone())
                    backdropArea = GlassFrame.FitBlurBounds(silhouette, source.ClientRectangle, GlassNative.RoundedCorners ? 8 : 0);
                revision = Theme.Revision;
                // Nonzero alpha preserves native hit testing, including child HWNDs.
                source.Opacity = 1.0 / 255;
                active = true;
                Position();
            }
            finally { rendering = false; }
        }
        private void Restore()
        {
            foreground.Hide();
            backdrop.Hide();
            active = false;
            if (!source.IsDisposed)
                source.Opacity = 1;
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Theme.Changed -= Queue;
            foreach (var control in new List<Control>(watched))
                Unwatch(control);
            ReleaseHandle();
            Restore();
            foreground.Dispose();
            backdrop.Dispose();
        }
    }
}
