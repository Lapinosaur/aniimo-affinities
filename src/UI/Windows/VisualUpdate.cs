using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace AniimoAffinities
{
    internal sealed class VisualUpdate : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr handle, IntPtr rectangle, IntPtr region, uint flags);
        private readonly PortableWindow window;
        private readonly List<Control> controls = new List<Control>();
        internal VisualUpdate(PortableWindow owner)
        {
            window = owner;
            if (window.VisualUpdateDepth++ == 0)
                Suspend(owner);
        }
        private void Suspend(Control control)
        {
            controls.Add(control);
            control.SuspendLayout();
            foreach (Control child in control.Controls)
                Suspend(child);
        }
        internal void LayoutNow()
        {
            foreach (Control control in controls)
                if (!control.IsDisposed)
                {
                    control.ResumeLayout(true);
                    control.PerformLayout();
                }
            foreach (Control control in controls)
                if (!control.IsDisposed)
                    control.SuspendLayout();
        }
        public void Dispose()
        {
            if (--window.VisualUpdateDepth != 0)
                return;
            foreach (Control control in controls)
                if (!control.IsDisposed)
                {
                    control.ResumeLayout(true);
                    control.PerformLayout();
                }
            if (!window.IsDisposed && window.IsHandleCreated)
            {
                // Finish painting the entire HWND tree before returning to the message loop.
                // An asynchronous Invalidate lets DWM present the previous wide surface
                // clipped to the new narrow window for one frame.
                RedrawWindow(window.Handle, IntPtr.Zero, IntPtr.Zero,
                    0x0001 | 0x0004 | 0x0080 | 0x0100 | 0x0400);
            }
        }
    }
}
