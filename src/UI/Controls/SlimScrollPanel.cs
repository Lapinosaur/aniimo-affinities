using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace AniimoAffinities
{
    // Default scrolling containers: native scrolling/focus behavior with the shared slim thumb.
    internal class SlimScrollPanel : Panel
    {
        internal SlimScrollPanel()
        {
            AutoScroll = true;
            SlimPanelScroll.Attach(this);
        }
    }
    internal class SlimFlowPanel : FlowLayoutPanel
    {
        internal SlimFlowPanel()
        {
            AutoScroll = true;
            SlimPanelScroll.Attach(this);
        }
    }
    internal sealed class SlimPanelScroll
    {
        private readonly ScrollableControl host;
        private readonly SlimScrollBar bar = new SlimScrollBar();
        private bool syncing;
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ScrollableControl, SlimPanelScroll> instances = new System.Runtime.CompilerServices.ConditionalWeakTable<ScrollableControl, SlimPanelScroll>();
        internal static void Attach(ScrollableControl host)
        {
            instances.Add(host, new SlimPanelScroll(host));
        }
        private SlimPanelScroll(ScrollableControl value)
        {
            host = value;
            bar.AccessibleName = "Scroll content";
            bar.ValueChanged += y => { host.AutoScrollPosition = new Point(0, y); Sync(); };
            host.ParentChanged += (s, e) => { bar.Parent = host.Parent; Sync(); };
            host.HandleCreated += (s, e) => Sync();
            host.Layout += (s, e) => Sync();
            host.Scroll += (s, e) => Sync();
            host.VisibleChanged += (s, e) => Sync();
            host.LocationChanged += (s, e) => Sync();
            host.Paint += (s, e) =>
            {
                if (!bar.Visible)
                    return;
                var state = e.Graphics.Save();
                e.Graphics.TranslateTransform(host.Width - 12, 0);
                bar.DrawScroll(e.Graphics);
                e.Graphics.Restore(state);
            };
            host.Disposed += (s, e) => bar.Dispose();
        }
        private void Sync()
        {
            if (syncing || host.IsDisposed || bar.IsDisposed || host.Parent == null)
                return;
            syncing = true;
            try
            {
                int extent = host.Controls.Cast<Control>().Select(control => control.Bottom - host.AutoScrollPosition.Y + control.Margin.Bottom).DefaultIfEmpty(0).Max();
                int maximum = Math.Max(0, extent - host.ClientSize.Height);
                if (host.IsHandleCreated)
                    NativeMethods.ShowScrollBar(host.Handle, 3, false);
                bar.BackColor = host.BackColor;
                bar.SetBounds(host.Right - 12, host.Top, 12, Math.Max(1, host.Height));
                bar.Configure(-host.AutoScrollPosition.Y, maximum, host.ClientSize.Height);
                bar.Visible = host.Visible && maximum > 0;
                bar.BringToFront();
            }
            finally { syncing = false; }
        }
    }
}
