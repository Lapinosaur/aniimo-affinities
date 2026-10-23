using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class RadarRenderer
    {
        internal sealed class Series
        {
            internal TeamMember Member;
            internal int Slot;
        }
        internal static double ComparisonMaximum(Series[] series)
        {
            return series.Length == 0 ? 1.6 : Math.Max(1, series.Max(item => Model.Factors(item.Member.Main, item.Member.Sub).Max()));
        }
        private static readonly int[] order = new[] { "Dark", "Lightning", "Fire", "Grass", "Light", "Ice", "Earth", "Water", "Wind" }.Select(name => Array.IndexOf(Model.Names, name)).ToArray();
        private static readonly double[] levels = { .625, 1, 1.6, 2.56 };
        private static readonly PointF[] directions = Enumerable.Range(0, 9).Select(i =>
        {
            double angle = -Math.PI / 2 + i * Math.PI * 2 / 9;
            return new PointF((float)Math.Cos(angle), (float)Math.Sin(angle));
        }).ToArray();

        internal static float RadiusFraction(double value, double maximum = 2.56)
        {
            return (float)Math.Max(0, Math.Min(1, value / maximum));
        }
        internal static double SharedMaximum(Selection selection)
        {
            return selection.Main < 0 ? 1.6 : Math.Max(1, Model.Factors(selection.Main, selection.Sub).Max());
        }
        internal static float FitRadius(SizeF content)
        {
            // Leave room for the type icons and their labels, not a second
            // padding band between the data and the icons.
            return Math.Max(30, Math.Min(content.Width / 2 - 66, content.Height / 2 - 51));
        }
        private static PointF Vertex(PointF center, float radius, int index)
        {
            return new PointF(center.X + directions[index].X * radius, center.Y + directions[index].Y * radius);
        }
        internal static void DrawRadar(Graphics g, PointF center, float radius, bool attack, bool defense, Selection selection, int highlightedTypes = 0, Series[] comparison = null, int highlightedSlot = -1)
        {
            bool comparing = comparison != null && comparison.Length > 0;
            double maximum = comparing ? ComparisonMaximum(comparison) : SharedMaximum(selection);
            int count = 0;
            while (count < levels.Length && levels[count] <= maximum + .0001)
                count++;
            var rings = new PointF[count][];
            for (int band = 0; band < count; band++)
            {
                rings[band] = new PointF[9];
                for (int i = 0; i < 9; i++)
                    rings[band][i] = Vertex(center, radius * RadiusFraction(levels[band], maximum), i);
            }
            // Paint outside in so each interval gets a theme-aware alternating background.
            for (int band = count - 1; band >= 0; band--)
            {
                using (var fill = new SolidBrush(band % 2 == 0 ? Theme.InsetSurface : Theme.HoverSurface))
                    g.FillPolygon(fill, rings[band]);
            }
            using (var font = UiFonts.Create("Segoe UI", 7))
                for (int band = 0; band < count; band++)
                {
                    double level = levels[band];
                    using (var pen = new Pen(level == 1 ? Theme.StrongBorder : Theme.Border, 1))
                        g.DrawPolygon(pen, rings[band]);
                    Drawing.Text(g, Model.Factor(level), font, Theme.Muted, new RectangleF(center.X + 4, center.Y - radius * RadiusFraction(level, maximum) - 3, 42, 14), false);
                }
            using (var pen = new Pen(Theme.Border, 1))
                for (int i = 0; i < 9; i++)
                    g.DrawLine(pen, center, Vertex(center, radius, i));
            double[] values = comparing ? (comparison.Length == 1 ? Model.Factors(comparison[0].Member.Main, comparison[0].Member.Sub) : null) : selection.Main < 0 ? null : Model.Factors(selection.Main, selection.Sub);
            if (comparing)
            {
                foreach (var item in highlightedSlot < 0 ? comparison : comparison.OrderBy(item => item.Slot == highlightedSlot ? 1 : 0).ToArray())
                {
                    var factors = Model.Factors(item.Member.Main, item.Member.Sub);
                    var ink = Theme.ComparisonInk(item.Slot);
                    bool highlighted = item.Slot == highlightedSlot;
                    if (highlightedSlot >= 0 && !highlighted)
                        ink = Color.FromArgb(45, ink);
                    if (defense)
                        DrawSeries(g, center, radius, factors, 9, ink, maximum, true, highlighted);
                    if (attack)
                        DrawSeries(g, center, radius, factors, 0, ink, maximum, true, highlighted);
                }
            }
            else if (values != null)
            {
                if (defense)
                    DrawSeries(g, center, radius, values, 9, Theme.Defense, maximum);
                if (attack)
                    DrawSeries(g, center, radius, values, 0, Theme.Attack, maximum);
            }
            using (var font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold))
                for (int i = 0; i < 9; i++)
                {
                    var point = Vertex(center, radius + 28, i);
                    if ((highlightedTypes & (1 << order[i])) != 0)
                        Drawing.Box(g, new RectangleF(point.X - 15, point.Y - 23, 30, 30), Theme.AccentSurface, Theme.Accent, 8);
                    VectorIcon.Draw(g, order[i], new RectangleF(point.X - 11, point.Y - 19, 22, 22));
                    {
                        if (values != null && attack)
                            Drawing.Text(g, Model.Factor(values[order[i]]), font, Theme.MatchupInk(values[order[i]], false), new RectangleF(point.X - 35, point.Y + 5, 70, 15), true);
                        if (values != null && defense)
                            Drawing.Text(g, Model.Factor(values[9 + order[i]]), font, Theme.MatchupInk(values[9 + order[i]], true), new RectangleF(point.X - 35, point.Y + (attack ? 20 : 5), 70, 15), true);
                    }
                    if (attack && defense && values != null)
                    {
                        Glyphs.Draw(g, "atk", new RectangleF(point.X - 31, point.Y + 7, 10, 10), Theme.TextColor);
                        Glyphs.Draw(g, "def", new RectangleF(point.X - 31, point.Y + 22, 10, 10), Theme.TextColor);
                    }
                }
        }
        private static void DrawSeries(Graphics g, PointF center, float radius, double[] values, int offset, Color color, double maximum, bool comparing = false, bool highlighted = false)
        {
            var points = Enumerable.Range(0, 9).Select(i => Vertex(center, radius * RadiusFraction(values[offset + order[i]], maximum), i)).ToArray();
            using (var fill = new SolidBrush(Color.FromArgb((highlighted ? 32 : comparing ? 12 : 28) * color.A / 255, color)))
                g.FillPolygon(fill, points);
            using (var pen = new Pen(color, highlighted ? 3.5f : 2))
            {
                g.DrawPolygon(pen, points);
            }
            for (int i = 0; i < points.Length; i++)
                using (var brush = new SolidBrush(comparing ? color : Theme.MatchupInk(values[offset + order[i]], offset != 0)))
                {
                    var point = points[i];
                    if (offset == 0)
                        g.FillEllipse(brush, point.X - 3, point.Y - 3, 6, 6);
                    else
                        g.FillRectangle(brush, point.X - 3, point.Y - 3, 6, 6);
                }
        }
    }
}
