using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class Glyphs
    {
        internal static void Draw(Graphics g, string name, RectangleF box, Color color)
        {
            if (name == "atk" || name == "rows")
                color = Theme.Attack;
            else if (name == "def" || name == "columns")
                color = Theme.Defense;
            var state = g.Save();
            g.TranslateTransform(box.X, box.Y);
            g.ScaleTransform(box.Width / 24, box.Height / 24);
            using (var pen = new Pen(color, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                if (name == "matrix")
                {
                    using (var frame = Drawing.Round(new RectangleF(3, 3, 18, 18), 2))
                        g.DrawPath(pen, frame);
                    g.DrawLine(pen, 9, 3, 9, 21);
                    g.DrawLine(pen, 15, 3, 15, 21);
                    g.DrawLine(pen, 3, 9, 21, 9);
                    g.DrawLine(pen, 3, 15, 21, 15);
                }
                else if (name == "affinities")
                {
                    g.DrawRectangle(pen, 2, 8, 5, 8);
                    g.DrawRectangle(pen, 17, 8, 5, 8);
                    g.DrawLine(pen, 8, 12, 16, 12);
                    g.DrawLine(pen, 13, 9, 16, 12);
                    g.DrawLine(pen, 13, 15, 16, 12);
                }
                else if (name == "combined")
                {
                    g.DrawRectangle(pen, 3, 3, 12, 12);
                    g.DrawRectangle(pen, 9, 9, 12, 12);
                }
                else if (name == "separate")
                {
                    using (var left = Drawing.Round(new RectangleF(2, 3, 8, 18), 2))
                        g.DrawPath(pen, left);
                    using (var right = Drawing.Round(new RectangleF(14, 3, 8, 18), 2))
                        g.DrawPath(pen, right);
                }
                else if (name == "radar")
                {
                    var points = Enumerable.Range(0, 5).Select(i => new PointF(12 + (float)Math.Sin(i * Math.PI * 2 / 5) * 9, 12 - (float)Math.Cos(i * Math.PI * 2 / 5) * 9)).ToArray();
                    g.DrawPolygon(pen, points);
                    g.DrawPolygon(pen, new[] { new PointF(12, 7), new PointF(18, 10), new PointF(15, 17), new PointF(8, 17), new PointF(9, 11) });
                }
                else if (name == "rows" || name == "columns")
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var frame = Drawing.Round(new RectangleF(3, 3, 18, 18), 2))
                        g.DrawPath(pen, frame);
                    for (int i = 0; i < 2; i++)
                    {
                        float divider = 9 + i * 6;
                        if (name == "rows")
                            g.DrawLine(pen, 3, divider, 21, divider);
                        else
                            g.DrawLine(pen, divider, 3, divider, 21);
                    }
                }
                else if (name == "moon")
                {
                    using (var moon = new GraphicsPath())
                    {
                        moon.AddBezier(15, 3, 4, 0, 0, 16, 9, 21);
                        moon.AddBezier(9, 21, 15, 24, 21, 19, 22, 14);
                        moon.AddBezier(22, 14, 13, 19, 8, 9, 15, 3);
                        moon.CloseFigure();
                        g.DrawPath(pen, moon);
                    }
                }
                else if (name == "system")
                {
                    g.DrawRectangle(pen, 3, 4, 18, 13);
                    g.DrawLine(pen, 12, 17, 12, 21);
                    g.DrawLine(pen, 8, 21, 16, 21);
                }
                else if (name == "sun")
                {
                    g.DrawEllipse(pen, 7, 7, 10, 10);
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4;
                        g.DrawLine(pen, 12 + (float)Math.Cos(a) * 8, 12 + (float)Math.Sin(a) * 8, 12 + (float)Math.Cos(a) * 10, 12 + (float)Math.Sin(a) * 10);
                    }
                }
                else if (name == "def")
                    g.DrawPolygon(pen, new[] { new PointF(12, 2), new PointF(20, 6), new PointF(20, 13), new PointF(17, 19), new PointF(12, 22), new PointF(7, 19), new PointF(4, 13), new PointF(4, 6) });
                else if (name == "atk")
                {
                    g.DrawLines(pen, new[] { new PointF(4, 3), new PointF(8, 4), new PointF(19, 16), new PointF(16, 19), new PointF(4, 7), new PointF(4, 3) });
                    g.DrawLine(pen, 14, 19, 20, 13);
                    g.DrawLine(pen, 18, 18, 22, 22);
                    g.DrawLines(pen, new[] { new PointF(20, 3), new PointF(16, 4), new PointF(12, 8) });
                    g.DrawLine(pen, 20, 3, 20, 7);
                    g.DrawLine(pen, 20, 7, 16, 11);
                    g.DrawLine(pen, 9, 14, 5, 18);
                    g.DrawLine(pen, 4, 13, 10, 19);
                    g.DrawLine(pen, 6, 18, 2, 22);
                }
                else if (name == "pin")
                {
                    g.DrawPolygon(pen, new[] { new PointF(8, 3), new PointF(16, 3), new PointF(16, 10), new PointF(19, 14), new PointF(5, 14), new PointF(8, 10) });
                    g.DrawLine(pen, 12, 14, 12, 21);
                }
                else if (name == "lock")
                {
                    g.DrawArc(pen, 7, 3, 10, 10, 180, 180);
                    g.DrawLine(pen, 7, 8, 7, 10);
                    g.DrawLine(pen, 17, 8, 17, 10);
                    using (var body = Drawing.Round(new RectangleF(5, 10, 14, 11), 2))
                        g.DrawPath(pen, body);
                    g.DrawLine(pen, 12, 14, 12, 17);
                }
                else if (name == "help")
                {
                    g.DrawEllipse(pen, 3, 3, 18, 18);
                    using (var mark = new GraphicsPath())
                    {
                        mark.AddBezier(9, 9, 9, 5, 16, 6, 15, 10);
                        mark.AddBezier(15, 10, 14, 12, 12, 11, 12, 14);
                        g.DrawPath(pen, mark);
                    }
                    g.DrawLine(pen, 12, 17, 12, 17.2f);
                }
                else if (name == "minimize")
                    g.DrawLine(pen, 5, 12, 19, 12);
                else if (name == "settings")
                {
                    var points = new PointF[24];
                    for (int i = 0; i < points.Length; i++)
                    {
                        double angle = (i * 15 - 7.5) * Math.PI / 180;
                        float radius = i % 4 == 1 || i % 4 == 2 ? 9.5f : 7.5f;
                        points[i] = new PointF(12 + (float)Math.Cos(angle) * radius, 12 + (float)Math.Sin(angle) * radius);
                    }
                    g.DrawPolygon(pen, points);
                    g.DrawEllipse(pen, 8.5f, 8.5f, 7, 7);
                }
                else
                {
                    g.DrawLine(pen, 7, 7, 17, 17);
                    g.DrawLine(pen, 17, 7, 7, 17);
                }
            }
            g.Restore(state);
        }
    }
}
