using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class SettingsDialog
    {
        private readonly Timer heightTransition = new Timer { Interval = 15 };
        private readonly System.Diagnostics.Stopwatch heightClock = new System.Diagnostics.Stopwatch();
        private int heightFrom, heightTarget;
        private void TransitionHeight(int target)
        {
            if (heightTransition.Enabled && heightTarget == target)
                return;
            heightTransition.Stop();
            heightTarget = target;
            if (!Visible || !IsHandleCreated || ClientSize.Height == target)
            {
                ClientSize = new Size(ClientSize.Width, target);
                return;
            }
            heightFrom = ClientSize.Height;
            heightClock.Restart();
            heightTransition.Start();
        }
        private void AdvanceHeight()
        {
            double t = Math.Min(1, heightClock.Elapsed.TotalMilliseconds / 200);
            double eased = t * t * (3 - 2 * t);
            ClientSize = new Size(ClientSize.Width, (int)Math.Round(heightFrom + (heightTarget - heightFrom) * eased));
            if (t >= 1)
            {
                heightTransition.Stop();
                heightClock.Stop();
            }
        }
        private bool windowSection;
        private bool displaySection;
        private readonly Label[] navigationHints = new Label[5];
        private readonly SettingsButton[] clearButtons = new SettingsButton[KeyBindings.Actions.Length];
        internal bool KeyboardNavigation
        {
            get; private set;
        }
        private bool inputFilterRegistered;
        private Point lastPointerPosition;
        // Observe this dialog's input without consuming it, including keys handled by capture fields.
        public bool PreFilterMessage(ref Message message)
        {
            var target = Control.FromHandle(message.HWnd);
            if (target == null || target.FindForm() != this)
                return false;
            bool keyboard = message.Msg == 0x100 || message.Msg == 0x104;
            bool mouseButton = message.Msg == 0x201 || message.Msg == 0x204 || message.Msg == 0x207 || message.Msg == 0x20B || message.Msg == 0x20A || message.Msg == 0x20E;
            bool mouseMove = message.Msg == 0x200 && Cursor.Position != lastPointerPosition;
            if (keyboard)
            {
                lastPointerPosition = Cursor.Position;
                if (!KeyboardNavigation)
                {
                    KeyboardNavigation = true;
                    RefreshNavigationHints();
                }
            }
            else if (mouseButton || mouseMove)
            {
                lastPointerPosition = Cursor.Position;
                if (KeyboardNavigation)
                {
                    KeyboardNavigation = false;
                    RefreshNavigationHints();
                }
            }
            return false;
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                    return;
                lastPointerPosition = Cursor.Position;
                if (!inputFilterRegistered)
                {
                    Application.AddMessageFilter(this);
                    inputFilterRegistered = true;
                }
                for (int i = 0; i < navigationHints.Length; i++)
                {
                    var badge = UiFonts.Own(new Label { AutoSize = false, Height = 14, TabStop = false, BackColor = Theme.Border, ForeColor = Theme.TextColor, Font = UiFonts.Create("Segoe UI", 6.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Visible = false });
                    navigationHints[i] = badge;
                    Controls.Add(badge);
                }
                foreach (var control in TabControls())
                    control.GotFocus += (s, a) => RefreshNavigationHints();
                foreach (var field in fields)
                    field.GotFocus += (s, a) => RefreshNavigationHints();
                Activated += (s, a) => RefreshNavigationHints();
                Deactivate += (s, a) => { foreach (var badge in navigationHints) badge.Visible = false; };
                RefreshNavigationHints();
            }));
        }
        // Follow WinForms traversal rather than maintaining a separate shortcut order.
        private Control[] TabControls()
        {
            var controls = new System.Collections.Generic.List<Control>();
            Control current = GetNextControl(this, true);
            while (current != null)
            {
                if (current.TabStop && current.CanSelect)
                    controls.Add(current);
                current = GetNextControl(current, true);
            }
            return controls.ToArray();
        }
        private void RefreshNavigationHints()
        {
            Invalidate(true);
            if (navigationHints[0] == null)
                return;
            var controls = TabControls();
            int focus = Array.FindIndex(controls, c => c.Focused);
            foreach (var badge in navigationHints)
                badge.Visible = false;
            if (Form.ActiveForm != this || !KeyboardNavigation || !DisplaySettings.ShowBadges)
                return;
            int fieldIndex = Array.FindIndex(fields, field => field.Focused);
            if (fieldIndex >= 0)
                PlaceHint(navigationHints[4], clearButtons[fieldIndex], "Del", 24);
            if (focus < 0 || controls.Length < 2)
                return;
            int[] indices = { (focus + controls.Length - 1) % controls.Length, focus, (focus + 1) % controls.Length };
            string[] labels = { "Shift+Tab", "Space", "Tab" };
            for (int i = 0; i < 3; i++)
            {
                var target = controls[indices[i]];
                var badge = navigationHints[i];
                var origin = PointToClient(target.PointToScreen(Point.Empty));
                badge.Text = labels[i];
                badge.Width = i == 0 ? 52 : i == 1 ? 35 : 25;
                badge.Location = new Point(Math.Max(0, origin.X + target.Width - badge.Width - 2), Math.Max(0, origin.Y - 8));
                badge.Visible = true;
                badge.BringToFront();
            }
        }
        private void PlaceHint(Label badge, Control target, string text, int width)
        {
            var origin = PointToClient(target.PointToScreen(Point.Empty));
            badge.Text = text;
            badge.Width = width;
            badge.Location = new Point(origin.X + target.Width - width, Math.Max(1, origin.Y - 8));
            badge.Visible = true;
            badge.BringToFront();
        }

    }
}
