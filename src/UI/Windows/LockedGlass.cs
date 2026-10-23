using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    // Locked windows retain their hotkey and separate unlock controls. Native
    // blur surfaces and an opaque foreground are both click-through.
    internal sealed class LockedGlass : IDisposable
    {
        private readonly Form source;
        private readonly GlassPool pool;
        private readonly List<GlassWindow> backdrops = new List<GlassWindow>();
        private readonly GlassWindow foreground = new GlassWindow();
        private readonly HashSet<Control> watched = new HashSet<Control>();
        private bool dirty = true, rendering, disposed, active, queued, failed;
        private Point presentedAt;
        private int revision = -1;
        internal bool Active
        {
            get
            {
                return active;
            }
        }

        internal LockedGlass(Form source, GlassPool pool = null)
        {
            this.source = source;
            this.pool = pool;
            Watch(source);
            source.LocationChanged += Move;
            Theme.Changed += ThemeChanged;
            Render();
        }
        private void Watch(Control control)
        {
            if (!watched.Add(control))
                return;
            control.Invalidated += Invalidated;
            control.SizeChanged += Changed;
            control.VisibleChanged += Changed;
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
            control.ControlAdded -= Added;
            control.ControlRemoved -= Removed;
            foreach (Control child in control.Controls)
                Unwatch(child);
        }
        private void Removed(object sender, ControlEventArgs e)
        {
            Unwatch(e.Control);
            Queue();
        }
        private void Added(object sender, ControlEventArgs e)
        {
            Watch(e.Control);
            Queue();
        }
        private void Invalidated(object sender, InvalidateEventArgs e)
        {
            if (!rendering)
                Queue();
        }
        private void Changed(object sender, EventArgs e)
        {
            Queue();
        }
        private void ThemeChanged()
        {
            Queue();
        }
        private void Queue()
        {
            dirty = true;
            if (queued || rendering || disposed || failed || !source.IsHandleCreated)
                return;
            queued = true;
            source.BeginInvoke(new Action(() =>
            {
                queued = false;
                Render();
            }));
        }
        private void Move(object sender, EventArgs e)
        {
            if (!active || disposed)
                return;
            var position = source.PointToScreen(Point.Empty);
            var delta = new Size(position.X - presentedAt.X, position.Y - presentedAt.Y);
            var windows = new List<GlassWindow>(backdrops) { foreground };
            var bounds = windows.ConvertAll(window =>
            {
                var rectangle = window.Bounds;
                rectangle.Offset(delta.Width, delta.Height);
                return rectangle;
            });
            GlassNative.PositionTogether(windows.ConvertAll(window => window.Handle).ToArray(), bounds.ToArray(), false);
            presentedAt = position;
        }
        private void Render()
        {
            if (disposed || rendering || failed || source.IsDisposed)
                return;
            if (!source.Visible || source.WindowState == FormWindowState.Minimized)
            {
                foreach (var backdrop in backdrops)
                    backdrop.Hide();
                foreground.Hide();
                dirty = true;
                return;
            }
            if (!dirty && revision == Theme.Revision)
                return;
            rendering = true;
            foreach (var surface in backdrops)
                surface.Below = IntPtr.Zero;
            try
            {
                if (SystemInformation.HighContrast)
                {
                    Restore();
                    failed = true;
                    return;
                }
                if (source.ClientSize.Width < 1 || source.ClientSize.Height < 1)
                    return;
                using (var image = new Bitmap(source.ClientSize.Width, source.ClientSize.Height))
                {
                    source.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    var position = source.PointToScreen(Point.Empty);
                    using (var region = GlassFrame.Prepare(image))
                    {
                        foreground.Bounds = new Rectangle(position, image.Size);
                        if (!GlassNative.Present(foreground.Handle, image, position))
                        {
                            Restore();
                            failed = true;
                            return;
                        }
                        if (!foreground.Visible)
                            foreground.Show();
                        GlassNative.Raise(foreground.Handle);
                        var surfaces = GlassFrame.Surfaces(region);
                        while (backdrops.Count > surfaces.Count)
                        {
                            Release(backdrops[backdrops.Count - 1]);
                            backdrops.RemoveAt(backdrops.Count - 1);
                        }
                        for (int i = 0; i < surfaces.Count; i++)
                        {
                            bool added = i == backdrops.Count;
                            if (added)
                                backdrops.Add(pool == null ? new GlassWindow() : pool.Take());
                            var backdrop = backdrops[i];

                            if (backdrop.MaterialRevision != Theme.Revision && !GlassNative.Enable(backdrop.Handle, Theme.Surface))
                            {
                                Restore();
                                failed = true;
                                return;
                            }
                            backdrop.MaterialRevision = Theme.Revision;
                            var rectangle = GlassFrame.FitBlurBounds(region, surfaces[i], GlassNative.RoundedCorners ? 8 : 0);
                            rectangle.Offset(position);
                            backdrop.PrepareBounds(rectangle);
                        }
                        // Prepare every native surface before exposing any of
                        // them: matrix cells must not appear one by one while
                        // their windows and materials are still being created.
                        GlassNative.PositionTogether(backdrops.ConvertAll(window => window.Handle).ToArray(), backdrops.ConvertAll(window => window.Bounds).ToArray(), true);
                        foreach (var backdrop in backdrops)
                            backdrop.MarkVisible();
                    }
                    foreach (var backdrop in backdrops)
                        backdrop.Below = foreground.Handle;
                    revision = Theme.Revision;
                    GlassNative.Raise(foreground.Handle);
                    // Locked content is noninteractive; its visual foreground
                    // remains opaque while the original host is fully hidden.
                    source.Opacity = 0;
                    presentedAt = position;
                    active = true;
                }
                dirty = false;
            }
            finally { rendering = false; }
        }
        private void Restore()
        {
            foreach (var backdrop in backdrops)
                if (pool == null)
                    backdrop.Hide();
                else
                    pool.Park(backdrop);
            foreground.Hide();
            if (!source.IsDisposed)
                source.Opacity = 1;
            active = false;
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Theme.Changed -= ThemeChanged;
            source.LocationChanged -= Move;
            foreach (var control in new List<Control>(watched))
                Unwatch(control);
            Restore();
            foreground.Dispose();
            foreach (var backdrop in backdrops)
                Release(backdrop);
        }
        private void Release(GlassWindow window)
        {
            if (pool == null)
                window.Dispose();
            else
                pool.Return(window);
        }
        internal sealed class GlassWindow : NativeWindow, IDisposable
        {
            internal IntPtr Below;
            internal int MaterialRevision = -1;
            internal void PrepareBounds(Rectangle value)
            {
                bounds = value;
            }
            internal void MarkVisible()
            {
                Visible = true;
            }
            private Rectangle bounds;
            internal bool Visible
            {
                get; private set;
            }
            internal GlassWindow()
            {
                CreateHandle(new CreateParams
                {
                    Caption = "Affinisaur glass",
                    Style = unchecked((int)0x80000000),
                    ExStyle = 0x08000000 | 0x80 | 0x20 | 0x8 | 0x80000
                });
            }
            internal Rectangle Bounds
            {
                get
                {
                    return bounds;
                }
                set
                {
                    bounds = value;
                    GlassNative.Position(Handle, value, false);
                }
            }
            internal void Show()
            {
                GlassNative.Position(Handle, bounds, true);
                Visible = true;
            }
            internal void Hide()
            {
                GlassNative.Hide(Handle);
                Visible = false;
            }
            public void Dispose()
            {
                DestroyHandle();
            }
            protected override void WndProc(ref Message message)
            {
                if (message.Msg == 0x47)
                {
                    bounds = GlassNative.WindowBounds(Handle);
                    base.WndProc(ref message);
                    if (Below != IntPtr.Zero && GlassNative.ZOrderChanged(message.LParam))
                        GlassNative.Raise(Below);
                    return;
                }
                if (message.Msg == 0x84)
                {
                    message.Result = new IntPtr(-1);
                    return;
                }
                if (message.Msg == 0x21)
                {
                    message.Result = new IntPtr(3);
                    return;
                }
                base.WndProc(ref message);
            }
        }
    }
}
