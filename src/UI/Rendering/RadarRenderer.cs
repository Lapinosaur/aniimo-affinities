using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class RadarRenderer
    {
        private static readonly int[] order = new[] { "Dark", "Lightning", "Fire", "Grass", "Light", "Ice", "Earth", "Water", "Wind" }.Select(name => Array.IndexOf(Model.Names, name)).ToArray();

        internal static float RadiusFraction(double value)
        {
            return (float)Math.Max(0, Math.Min(1, value / 2.56));
        }
        private static PointF Vertex(PointF center, float radius, int index)
        {
            double angle = -Math.PI / 2 + index * Math.PI * 2 / 9;
            return new PointF(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius);
        }
        internal static void DrawRadar(Graphics g, PointF center, float radius, bool attack, bool defense, Selection selection)
        {
            var levels = new[] { 0.625, 1, 1.6, 2.56 };
            // Paint outside in so each interval gets a theme-aware alternating background.
            for (int band = levels.Length - 1; band >= 0; band--)
            {
                var polygon = Enumerable.Range(0, 9).Select(i => Vertex(center, radius * RadiusFraction(levels[band]), i)).ToArray();
                using (var fill = new SolidBrush(band % 2 == 0 ? Theme.Background : Theme.HoverSurface))
                    g.FillPolygon(fill, polygon);
            }
            foreach (double level in levels)
            {
                var ring = Enumerable.Range(0, 9).Select(i => Vertex(center, radius * RadiusFraction(level), i)).ToArray();
                using (var pen = new Pen(level == 1 ? Theme.StrongBorder : Theme.Border, 1))
                    g.DrawPolygon(pen, ring);
                using (var font = UiFonts.Create("Segoe UI", 7))
                    Drawing.Text(g, Model.Factor(level), font, Theme.Muted, new RectangleF(center.X + 4, center.Y - radius * RadiusFraction(level) - 3, 42, 14), false);
            }
            using (var pen = new Pen(Theme.Border, 1))
                for (int i = 0; i < 9; i++)
                    g.DrawLine(pen, center, Vertex(center, radius, i));
            double[] values = selection.Main < 0 ? null : Model.Factors(selection.Main, selection.Sub);
            if (values != null)
            {
                if (defense)
                    DrawSeries(g, center, radius, values, 9, Theme.Defense);
                if (attack)
                    DrawSeries(g, center, radius, values, 0, Theme.Attack);
            }
            for (int i = 0; i < 9; i++)
            {
                var point = Vertex(center, radius + 42, i);
                VectorIcon.Draw(g, order[i], new RectangleF(point.X - 11, point.Y - 19, 22, 22));
                using (var font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold))
                {
                    if (values != null && attack)
                        Drawing.Text(g, Model.Factor(values[order[i]]), font, Theme.Attack, new RectangleF(point.X - 35, point.Y + 5, 70, 15), true);
                    if (values != null && defense)
                        Drawing.Text(g, Model.Factor(values[9 + order[i]]), font, Theme.Defense, new RectangleF(point.X - 35, point.Y + (attack ? 20 : 5), 70, 15), true);
                }
            }
        }
        private static void DrawSeries(Graphics g, PointF center, float radius, double[] values, int offset, Color color)
        {
            var points = Enumerable.Range(0, 9).Select(i => Vertex(center, radius * RadiusFraction(values[offset + order[i]]), i)).ToArray();
            using (var fill = new SolidBrush(Color.FromArgb(28, color)))
                g.FillPolygon(fill, points);
            using (var pen = new Pen(color, 2))
                g.DrawPolygon(pen, points);
            using (var brush = new SolidBrush(color))
                foreach (var point in points)
                    g.FillEllipse(brush, point.X - 3, point.Y - 3, 6, 6);
        }
    }
}
