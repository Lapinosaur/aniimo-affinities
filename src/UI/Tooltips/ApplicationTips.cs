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
        private readonly Timer timer = new Timer();
        private readonly TipBubble bubble = new TipBubble();
        private Control anchor; private Form owner; private bool displayed;
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
            if (!labels.ContainsKey(control))
            {
                control.MouseEnter += Enter;
                control.MouseLeave += Leave;
                control.MouseDown += Down;
                control.Disposed += Leave;
            }
            labels[control] = text;
        }
        private void Enter(object sender, EventArgs e)
        {
            Hide();
            anchor = (Control)sender;
            timer.Interval = 450;
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
            bubble.Present(anchor, text);
            displayed = true;
            timer.Interval = 8000;
            timer.Start();
        }
        private void Hide()
        {
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
            Hide();
            timer.Dispose();
            bubble.Dispose();
            foreach (var item in labels)
            {
                item.Key.MouseEnter -= Enter;
                item.Key.MouseLeave -= Leave;
                item.Key.MouseDown -= Down;
                item.Key.Disposed -= Leave;
            }
            labels.Clear();
        }
    }
}
