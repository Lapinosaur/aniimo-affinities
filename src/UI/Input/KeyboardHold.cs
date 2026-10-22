using System;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class KeyboardHold
    {
        internal bool Active
        {
            get; private set;
        }
        private Form owner;
        private readonly Action cancel;
        internal KeyboardHold(Button button, Action start, Action cancel)
        {
            this.cancel = cancel;
            button.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Space)
                    return;
                e.Handled = e.SuppressKeyPress = true;
                if (Active || e.Modifiers != Keys.None)
                    return;
                Active = true;
                owner = button.FindForm();
                if (owner != null)
                    owner.Deactivate += Deactivate;
                start();
            };
            button.KeyUp += (s, e) =>
            {
                if (e.KeyCode != Keys.Space)
                    return;
                e.Handled = e.SuppressKeyPress = true;
                bool click = Active;
                Reset();
                if (click)
                    button.PerformClick();
            };
            button.LostFocus += (s, e) => Reset();
            button.EnabledChanged += (s, e) => { if (!button.Enabled) Reset(); };
            button.VisibleChanged += (s, e) => { if (!button.Visible) Reset(); };
            button.Disposed += (s, e) => Reset();
        }
        private void Deactivate(object sender, EventArgs e)
        {
            Reset();
        }
        private void Reset()
        {
            if (owner != null)
                owner.Deactivate -= Deactivate;
            owner = null;
            if (!Active)
                return;
            Active = false;
            cancel();
        }
    }
}
