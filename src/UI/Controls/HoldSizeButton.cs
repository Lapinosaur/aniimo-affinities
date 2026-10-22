using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace AniimoAffinities
{
    internal sealed class HoldSizeButton : Button
    {
        internal float Target;
        internal event Action<float> Commit;
        private readonly Timer timer = new Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private float progress;
        internal HoldSizeButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            timer.Tick += (s, e) =>
            {
                progress = Math.Min(1, (float)watch.Elapsed.TotalMilliseconds / 900);
                Invalidate();
                if (progress >= 1)
                {
                    float target = Target;
                    CancelHold();
                    if (Commit != null)
                        Commit(target);
                }
            };
        }
        internal void CancelHold()
        {
            timer.Stop();
            watch.Reset();
            progress = 0;
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                watch.Restart();
                timer.Start();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            CancelHold();
            base.OnMouseUp(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            CancelHold();
            base.OnMouseLeave(e);
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (!Capture)
                CancelHold();
            base.OnMouseCaptureChanged(e);
        }
        protected override void OnClick(EventArgs e)
        {
        } // A click alone must never change scale.
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Drawing.Box(g, new RectangleF(1, 1, Width - 3, Height - 3), Theme.Surface, Theme.Border, 7);
            using (var font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold))
                Drawing.Text(g, Text, font, Theme.TextColor, new RectangleF(0, 0, Width, Height), true);
            Drawing.HoldHint(g, new RectangleF(1, 1, Width - 3, Height - 3));
            if (progress <= 0)
                return;
            using (var path = Drawing.Round(new RectangleF(1, 1, Width - 3, Height - 3), 7))
            using (var pen = new Pen(Theme.Accent, 2))
            {
                path.Flatten();
                var points = path.PathPoints;
                float length = 0;
                for (int i = 0; i < points.Length; i++)
                    length += Distance(points[i], points[(i + 1) % points.Length]);
                float remaining = length * progress;
                for (int i = 0; i < points.Length && remaining > 0; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Length];
                    float distance = Distance(a, b);
                    if (distance <= 0)
                        continue;
                    float fraction = Math.Min(1, remaining / distance);
                    g.DrawLine(pen, a, new PointF(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction));
                    remaining -= distance;
                }
            }
        }
        private static float Distance(PointF a, PointF b)
        {
            return (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
