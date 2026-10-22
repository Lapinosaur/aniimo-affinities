using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class MatrixRenderer
    {
        internal static readonly int[] Order = new[] { "Fire", "Water", "Grass", "Lightning", "Ice", "Earth", "Wind", "Light", "Dark" }.Select(name => Array.IndexOf(Model.Names, name)).ToArray();

        internal static void DrawCell(Graphics g, int row, int column, Rectangle area, int fixedType, int hoverRow = -1, int hoverColumn = -1, bool highlight = false)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool tracked = (hoverRow > 0 && row == hoverRow) || (hoverColumn > 0 && column == hoverColumn);
            Color fill = tracked ? Theme.AccentSurface : Theme.Surface, border = tracked ? Theme.Accent : Theme.Border;
            if (row == 0 || column == 0)
            {
                if (row != column && !tracked)
                {
                    fill = row == 0 ? Theme.DefenseSurface : Theme.AttackSurface;
                    border = row == 0 ? Theme.Defense : Theme.Attack;
                }
                Drawing.Box(g, new RectangleF(1, 1, area.Width - 2, area.Height - 2), fill, border, 8);
                if (row == 0 && column == 0)
                {
                    using (var outline = Drawing.Round(new RectangleF(1, 1, area.Width - 2, area.Height - 2), 8))
                    using (var lower = new System.Drawing.Drawing2D.GraphicsPath())
                    using (var upper = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        lower.AddPolygon(new[] { new PointF(0, 0), new PointF(area.Width, area.Height), new PointF(0, area.Height) });
                        upper.AddPolygon(new[] { new PointF(0, 0), new PointF(area.Width, 0), new PointF(area.Width, area.Height) });
                        var state = g.Save();
                        g.SetClip(lower, System.Drawing.Drawing2D.CombineMode.Intersect);
                        using (var pen = new Pen(Theme.Attack, 1))
                            g.DrawPath(pen, outline);
                        g.Restore(state);
                        state = g.Save();
                        g.SetClip(upper, System.Drawing.Drawing2D.CombineMode.Intersect);
                        using (var pen = new Pen(Theme.Defense, 1))
                            g.DrawPath(pen, outline);
                        g.Restore(state);
                    }
                    using (var divider = new Pen(Theme.Border, 1))
                        g.DrawLine(divider, 6, 4, area.Width - 6, area.Height - 4);
                    Glyphs.Draw(g, "atk", new RectangleF(6, area.Height - 22, 18, 18), Theme.Muted);
                    Glyphs.Draw(g, "def", new RectangleF(area.Width - 24, 4, 18, 18), Theme.Muted);
                    return;
                }
                int type = Order[(row == 0 ? column : row) - 1];
                bool pair = row == 0 && fixedType >= 0 && fixedType != type;
                VectorIcon.Draw(g, type, new RectangleF(pair ? 7 : 20, 8, 24, 24));
                if (pair)
                    VectorIcon.Draw(g, fixedType, new RectangleF(34, 8, 24, 24));
                return;
            }
            int attack = Order[row - 1], defense = Order[column - 1];
            double value = Model.Factors(defense, fixedType)[9 + attack];
            bool best = Model.MatrixBest(fixedType, attack, defense);
            bool muted = highlight && !best;
            fill = muted ? Theme.Surface : Theme.MultiplierFill(value);
            border = tracked || highlight && best ? Theme.Accent : muted ? Theme.Border : Theme.MultiplierBorder(value);
            Drawing.Box(g, new RectangleF(1, 1, area.Width - 2, area.Height - 2), fill, border, 8);
            Color ink = muted ? Theme.Muted : Theme.MultiplierInk(value);
            using (var font = UiFonts.Create("Segoe UI", 10, FontStyle.Bold))
                Drawing.Text(g, Model.Factor(value), font, ink, area, true);
        }
    }
}
