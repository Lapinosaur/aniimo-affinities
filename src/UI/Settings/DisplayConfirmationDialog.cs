using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class DisplayConfirmationDialog : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        private readonly Timer countdown = new Timer { Interval = 33 };
        private readonly System.Diagnostics.Stopwatch elapsed = new System.Diagnostics.Stopwatch();
        internal DisplayConfirmationDialog(double seconds = 15)
        {
            Text = "Keep display settings?";
            ClientSize = new Size(440, 226);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(UiFonts.Own(new Label { Text = "Is everything readable?", Font = UiFonts.Create("Segoe UI", 13, FontStyle.Bold), Location = new Point(24, 54), Size = new Size(392, 32) }));
            var remaining = new Label { Location = new Point(24, 94), Size = new Size(392, 44), ForeColor = Theme.Muted };
            Controls.Add(remaining);
            var progress = new DrawingPanel { Location = new Point(24, 140), Size = new Size(392, 8), TabStop = false, AccessibleRole = AccessibleRole.ProgressBar, AccessibleName = "Time remaining before restoring previous settings" };
            progress.Paint += (s, e) =>
            {
                e.Graphics.Clear(Theme.Background);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                float fraction = (float)Math.Max(0, Math.Min(1, 1 - elapsed.Elapsed.TotalSeconds / seconds));
                using (var track = Drawing.Round(new RectangleF(0, 0, progress.Width, progress.Height), 4))
                {
                    using (var fill = new SolidBrush(Theme.Border))
                        e.Graphics.FillPath(fill, track);
                    var state = e.Graphics.Save();
                    e.Graphics.SetClip(track);
                    using (var fill = new SolidBrush(Theme.Accent))
                        e.Graphics.FillRectangle(fill, 0, 0, progress.Width * fraction, progress.Height);
                    e.Graphics.Restore(state);
                }
            };
            Controls.Add(progress);
            var revert = new SettingsButton { Text = "Revert", DialogResult = DialogResult.Cancel, Location = new Point(152, 164), Size = new Size(112, 36), TabIndex = 0 };
            var keep = new SettingsButton { Text = "Keep changes", Primary = true, DialogResult = DialogResult.OK, Location = new Point(272, 164), Size = new Size(144, 36), TabIndex = 1 };
            Controls.Add(revert);
            Controls.Add(keep);
            CancelButton = revert;
            AcceptButton = keep;
            Action refresh = () =>
            {
                double left = seconds - elapsed.Elapsed.TotalSeconds;
                remaining.Text = "Previous settings will be restored in " + Math.Max(0, (int)Math.Ceiling(left)) + " seconds.";
                progress.AccessibleDescription = Math.Max(0, (int)Math.Ceiling(left)) + " seconds remaining";
                progress.Invalidate();
                if (left <= 0)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
            countdown.Tick += (s, e) => refresh();
            Shown += (s, e) => { elapsed.Start(); refresh(); countdown.Start(); };
            FormClosed += (s, e) => countdown.Stop();
            refresh();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                countdown.Dispose();
            base.Dispose(disposing);
        }
    }
}
