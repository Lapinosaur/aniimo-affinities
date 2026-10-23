using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class Drawing
    {
        internal static void ProgressOutline(Graphics g, RectangleF bounds, float radius, float progress, Color color, float width)
        {
            using (var path = Round(bounds, radius))
            using (var pen = new Pen(color, width))
            {
                path.Flatten();
                var points = path.PathPoints;
                Func<PointF, PointF, float> distance = (a, b) => (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                float length = 0;
                for (int i = 0; i < points.Length; i++)
                    length += distance(points[i], points[(i + 1) % points.Length]);
                float remaining = length * Math.Max(0, Math.Min(1, progress));
                for (int i = 0; i < points.Length && remaining > 0; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Length];
                    float segment = distance(a, b);
                    if (segment <= 0)
                        continue;
                    float fraction = Math.Min(1, remaining / segment);
                    g.DrawLine(pen, a, new PointF(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction));
                    remaining -= segment;
                }
            }
        }
        internal static GraphicsPath Round(RectangleF r, float radius)
        {
            float d = Math.Min(Math.Max(0, radius * 2), Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            if (d <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        internal static void Box(Graphics g, RectangleF r, Color fill, Color border, float radius)
        {
            using (var path = Round(r, radius))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }
        internal static void HoldHint(Graphics g, RectangleF bounds)
        {
            // A short accent segment previews the contour that fills during a hold.
            using (var pen = new Pen(Color.FromArgb(160, Theme.AccentForeground), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(pen, bounds.X + bounds.Width / 2 - 3, bounds.Bottom - 1, bounds.X + bounds.Width / 2 + 3, bounds.Bottom - 1);
        }
        internal static void Text(Graphics g, string text, Font font, Color color, RectangleF rect, bool center)
        {
            using (var b = new SolidBrush(color))
            using (var f = new StringFormat { Alignment = center ? StringAlignment.Center : StringAlignment.Near, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, b, rect, f);
        }
        // Inputs: semantic icon and box. Output: vector shield, swords, reset or pin. Fallback: reset. Raises: none.
    }
}
