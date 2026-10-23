using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace AniimoAffinities
{
    // A bounded, per-host reserve. Native HWND creation happens in short idle
    // slices, not in the lock gesture. Prepared surfaces stay outside all screens.
    internal sealed class GlassPool : IDisposable
    {
        internal const int Capacity = 112;
        private readonly Stack<LockedGlass.GlassWindow> available = new Stack<LockedGlass.GlassWindow>();
        private readonly Timer timer = new Timer { Interval = 1 };
        private int created;
        private bool disposed;
        internal int ReadyCount
        {
            get
            {
                return available.Count;
            }
        }
        internal GlassPool()
        {
            timer.Tick += Warm;
            timer.Start();
        }
        private void Warm(object sender, EventArgs args)
        {
            var budget = Stopwatch.StartNew();
            do
            {
                if (created >= Capacity)
                {
                    timer.Stop();
                    return;
                }
                var window = new LockedGlass.GlassWindow();
                created++;
                if (GlassNative.Enable(window.Handle, Theme.Surface))
                    window.MaterialRevision = Theme.Revision;
                Park(window);
                available.Push(window);
            } while (budget.ElapsedMilliseconds < 8);
        }
        internal LockedGlass.GlassWindow Take()
        {
            if (available.Count > 0)
                return available.Pop();
            created++;
            return new LockedGlass.GlassWindow();
        }
        internal void Return(LockedGlass.GlassWindow window)
        {
            window.Below = IntPtr.Zero;
            if (disposed || available.Count >= Capacity)
            {
                window.Dispose();
                created--;
            }
            else
            {
                Park(window);
                available.Push(window);
            }
        }
        internal void Park(LockedGlass.GlassWindow window)
        {
            window.Below = IntPtr.Zero;
            var desktop = SystemInformation.VirtualScreen;
            var parked = new System.Drawing.Rectangle(desktop.Left - 4, desktop.Top - 4, 1, 1);
            if (window.Bounds != parked)
                window.Bounds = parked;
            if (!window.Visible)
                window.Show();
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            timer.Dispose();
            while (available.Count > 0)
                available.Pop().Dispose();
        }
    }
}
