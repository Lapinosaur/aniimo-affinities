using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class Drawing
    {
        internal static GraphicsPath Round(RectangleF r, float radius)
        {
            float d = radius * 2;
            var p = new GraphicsPath();
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
