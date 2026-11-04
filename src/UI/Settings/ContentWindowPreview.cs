using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class ContentWindowPreview : CheckBox
    {
        private readonly Timer animation = new Timer { Interval = 20 };
        private readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        private float progress = 1;
        internal ContentWindowPreview()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            PreviewCard.Attach(this);
            CheckedChanged += (s, e) => Replay();
            MouseEnter += (s, e) => Replay();
            MouseLeave += (s, e) => { animation.Stop(); progress = 1; Invalidate(); };
            animation.Tick += (s, e) => { float t = System.Math.Max(0, System.Math.Min(1, (clock.ElapsedMilliseconds - 180) / 700f)); progress = t * t * (3 - 2 * t); if (progress >= 1) animation.Stop(); Invalidate(); };
            AccessibleDescription = Language.T("When selected, minimizing the main window leaves radars and comparisons visible. Otherwise they minimize together.");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                animation.Dispose();
            base.Dispose(disposing);
            if (disposing)
                Events.Dispose();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Background);
            float zoom = WindowZoom.Factor(this);
            g.ScaleTransform(zoom, zoom);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Width / zoom, h = Height / zoom;
            PreviewCard.Draw(g, new RectangleF(0, 0, w - 1, h - 1), Checked, Enabled, ClientRectangle.Contains(PointToClient(Cursor.Position)), Focused && ShowFocusCues);
            var check = new RectangleF(14, 12, 16, 16);
            using (var fill = new SolidBrush(Checked && Enabled ? Theme.Accent : Theme.Surface))
                g.FillEllipse(fill, check);
            using (var pen = new Pen(Enabled ? Checked ? Theme.AccentForeground : Theme.StrongBorder : Theme.Border, 1.3f))
                g.DrawEllipse(pen, check);
            if (Checked)
                Glyphs.Draw(g, "check", new RectangleF(16, 14, 12, 12), Enabled ? Theme.AccentInk : Theme.Muted);
            Drawing.Text(g, Text, Font, Theme.TextColor, new RectangleF(38, 8, w - 274, 34), false);
            Drawing.Text(g, Language.T("Radars and tables stay visible."), Font, Theme.Muted, new RectangleF(38, 45, w - 274, 35), false);
            float x = w - 226;
            Drawing.Box(g, new RectangleF(x, 10, 212, 68), Theme.InsetSurface, Theme.Border, 6);
            DrawWindow(g, x + 10, progress, "main", Theme.Muted);
            Glyphs.Draw(g, "minimize", new RectangleF(x + 70, 37, 14, 14), Theme.Muted);
            DrawWindow(g, x + 88, Checked ? 0 : progress, "radar", Theme.AccentForeground);
            DrawWindow(g, x + 148, Checked ? 0 : progress, "matrix", Theme.Defense);
        }
        private void Replay()
        {
            progress = 0;
            clock.Restart();
            animation.Start();
            Invalidate();
        }
        private static void DrawWindow(Graphics g, float x, float collapse, string kind, Color color)
        {
            float top = 19 + 43 * collapse, height = 50 - 43 * collapse;
            var bounds = new RectangleF(x, top, 54, height);
            Drawing.Box(g, bounds, Theme.Surface, color, 3);
            var state = g.Save();
            g.SetClip(bounds, CombineMode.Intersect);
            using (var pen = new Pen(color, 1))
            {
                g.DrawLine(pen, x + 5, top + 7, x + 15, top + 7);
                g.DrawLine(pen, x + 43, top + 7, x + 48, top + 7);
                if (kind == "main")
                {
                    g.DrawRectangle(pen, x + 6, top + 16, 18, 25);
                    g.DrawRectangle(pen, x + 29, top + 16, 18, 25);
                }
                else if (kind == "matrix")
                {
                    for (int row = 0; row < 3; row++)
                        for (int col = 0; col < 4; col++)
                            using (var fill = new SolidBrush(Color.FromArgb((row + col) % 3 == 0 ? 170 : 55, color)))
                                g.FillRectangle(fill, x + 7 + col * 10, top + 17 + row * 9, 8, 7);
                }
                else
                {
                    PointF[] points = { new PointF(x + 27, top + 16), new PointF(x + 44, top + 27), new PointF(x + 37, top + 43), new PointF(x + 17, top + 43), new PointF(x + 10, top + 27) };
                    using (var fill = new SolidBrush(Color.FromArgb(45, color)))
                        g.FillPolygon(fill, points);
                    g.DrawPolygon(pen, points);
                    foreach (var point in points)
                        g.DrawLine(pen, x + 27, top + 31, point.X, point.Y);
                }
            }
            g.Restore(state);
        }
    }
}
