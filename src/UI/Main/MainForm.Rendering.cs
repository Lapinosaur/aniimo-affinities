using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class MainForm
    {
        private void PaintAffinities(object sender, PaintEventArgs e)
        {
            affinityHits.Clear();
            originHits.Clear();
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (preview.Main < 0 && currentView == "matrix")
            {
                DrawInlineMatrix(g, 8);
                return;
            }
            if (preview.Main < 0)
            {
                PaintEmptyState(g);
                return;
            }
            int a = preview.Main, b = preview.Sub;
            string name = Model.Names[a] + (b >= 0 ? " + " + Model.Names[b] : "");
            if (!TopMost)
            {
                VectorIcon.Draw(g, a, new RectangleF(18, 12, 36, 36));
                if (b >= 0)
                    VectorIcon.Draw(g, b, new RectangleF(64, 12, 36, 36));
                using (var title = UiFonts.Create("Segoe UI", 17, FontStyle.Bold))
                    Drawing.Text(g, name, title, Theme.TextColor, new RectangleF(b >= 0 ? 116 : 70, 5, 500, 35), false);
                Drawing.Text(g, hovered >= 0 && hovered != selection.Main && hovered != selection.Sub ? "Temporary preview" : "Type affinities", Font, Theme.Muted, new RectangleF(b >= 0 ? 118 : 72, 39, 300, 20), false);
            }
            float width = (canvas.Width - 36) / 2f;
            var factors = Model.Factors(a, b);
            float cardTop = TopMost ? 8 : 78;
            if (currentView == "matrix")
            {
                DrawInlineMatrix(g, cardTop);
                return;
            }
            for (int column = 0; column < 2; column++)
                DrawCard(g, new RectangleF(12 + column * (width + 12), cardTop, width, canvas.Height - cardTop - 12), column, factors, name);
        }
        private void DrawInlineMatrix(Graphics g, float cardTop)
        {
            float scale = Math.Min((canvas.Width - 32) / 680f, (canvas.Height - cardTop - 16) / 440f);
            float left = (canvas.Width - 680 * scale) / 2;
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 10; c++)
                {
                    var state = g.Save();
                    g.TranslateTransform(left + c * 68 * scale, cardTop + r * 44 * scale);
                    g.ScaleTransform(scale, scale);
                    MatrixRenderer.DrawCell(g, r, c, new Rectangle(0, 0, 64, 40), preview.Sub >= 0 ? preview.Main : -1);
                    g.Restore(state);
                }
        }
        // Inputs: shared projections. Output: two directed diagrams, grouping equal multipliers. Fallback: dash for empty groups. Raises: none.
        private void DrawCard(Graphics g, RectangleF box, int column, double[] factors, string name)
        {
            Drawing.Box(g, box, Theme.Surface, Theme.Border, Theme.PanelRadius);
            Drawing.Box(g, new RectangleF(box.X + 16, box.Y + 16, 32, 32), Theme.AccentSurface, Theme.AccentSurface, 9);
            Glyphs.Draw(g, column == 0 ? "atk" : "def", new RectangleF(box.X + 22, box.Y + 22, 20, 20), Theme.AccentForeground);
            using (var bold = UiFonts.Create("Segoe UI", 13, FontStyle.Bold))
                Drawing.Text(g, column == 0 ? "Attack" : "Defense", bold, column == 0 ? Theme.Attack : Theme.Defense, new RectangleF(box.X + 60, box.Y + 11, box.Width - 76, 27), false);
            using (var caption = UiFonts.Create("Segoe UI", 8.5f))
                Drawing.Text(g, column == 0 ? "Damage dealt · best available type" : "Damage received · combined types", caption, Theme.Muted, new RectangleF(box.X + 60, box.Y + 37, box.Width - 76, 20), false);
            using (var line = new Pen(Theme.Border))
                g.DrawLine(line, box.X + 16, box.Y + 68, box.Right - 16, box.Y + 68);
            float cy = box.Y + 68 + (box.Height - 68) / 2, cx = box.X + box.Width / 2;
            if (inlineRadar)
            {
                float radius = Math.Max(30, Math.Min(box.Width / 2 - 80, (box.Height - 68) / 2 - 65));
                RadarRenderer.DrawRadar(g, new PointF(cx, cy - 4), radius, column == 0, column == 1, preview);
                return;
            }
            float centerHeight = preview.Sub >= 0 ? 112 : 64;
            Drawing.Box(g, new RectangleF(cx - 29, cy - centerHeight / 2, 58, centerHeight), Theme.InsetSurface, Theme.Border, 16);
            if (preview.Sub >= 0)
            {
                HighlightOrigin(g, new RectangleF(cx - 22, cy - 49, 44, 44), column, 0);
                HighlightOrigin(g, new RectangleF(cx - 22, cy + 5, 44, 44), column, 1);
                VectorIcon.Draw(g, preview.Main, new RectangleF(cx - 18, cy - 45, 36, 36));
                VectorIcon.Draw(g, preview.Sub, new RectangleF(cx - 18, cy + 9, 36, 36));
                using (var line = new Pen(Theme.Border))
                    g.DrawLine(line, cx - 12, cy, cx + 12, cy);
            }
            else
            {
                HighlightOrigin(g, new RectangleF(cx - 23, cy - 23, 46, 46), column, 0);
                VectorIcon.Draw(g, preview.Main, new RectangleF(cx - 19, cy - 19, 38, 38));
            }
            using (var small = UiFonts.Create("Segoe UI", 8.5f, FontStyle.Bold))
                Drawing.Text(g, name.Replace(" + ", "\n"), small, Theme.TextColor, new RectangleF(cx - 54, cy + centerHeight / 2 + 10, 108, 38), true);
            for (int side = 0; side < 2; side++)
            {
                var entries = Enumerable.Range(0, 9).Where(t => side == 0 ? factors[column * 9 + t] < 1 : factors[column * 9 + t] > 1).ToArray();
                Color color = column == side ? Theme.Muted : Theme.AccentForeground;
                float outer = side == 0 ? box.X + 12 : box.Right - 66;
                DrawStack(g, entries, factors, column, outer, cy, box.Height - 100);
                float arrowX = side == 0 ? box.X + 78 : cx + 39, arrowW = box.Width / 2 - 117;
                if (entries.Length > 0)
                {
                    bool right = column == 0 ? side == 1 : side == 0;
                    using (var pen = new Pen(color, 1.3f))
                    {
                        float end = right ? arrowX + arrowW : arrowX;
                        g.DrawLine(pen, arrowX, cy, arrowX + arrowW, cy);
                        g.DrawLine(pen, end, cy, end + (right ? -6 : 6), cy - 6);
                        g.DrawLine(pen, end, cy, end + (right ? -6 : 6), cy + 6);
                    }
                    var multipliers = entries.Select(t => factors[column * 9 + t]).Distinct().OrderBy(multiplier => multiplier).ToArray();
                    string value = multipliers.Length == 1 ? Model.Factor(multipliers[0]) : Model.Factor(multipliers[0]) + "–" + Model.Factor(multipliers[multipliers.Length - 1]);
                    var valueBox = new RectangleF(arrowX - 7, cy - 40, arrowW + 14, 26);
                    Drawing.Box(g, valueBox, Theme.InsetSurface, Theme.Border, 7);
                    using (var bold = UiFonts.Create("Segoe UI", multipliers.Length == 1 ? 10 : 8.5f, FontStyle.Bold))
                        Drawing.Text(g, value, bold, color, valueBox, true);
                }
                string label = column == 0 ? (side == 0 ? "Not effective" : "Super effective") : (side == 0 ? "Resistance" : "Weakness");
                using (var small = UiFonts.Create("Segoe UI", 8, FontStyle.Regular))
                    Drawing.Text(g, label, small, color, new RectangleF(arrowX - 12, cy + 12, arrowW + 24, 34), true);
            }
        }
        private void HighlightOrigin(Graphics g, RectangleF bounds, int column, int origin)
        {
            originHits.Add(Tuple.Create(bounds, 1 << origin, column));
            if (affinityColumn != column || (affinityMask & (1 << origin)) == 0)
                return;
            Drawing.Box(g, bounds, Theme.AccentSurface, Theme.Accent, 12);
        }
        private void DrawStack(Graphics g, int[] entries, double[] values, int column, float x, float cy, float available)
        {
            if (entries.Length == 0)
            {
                Drawing.Text(g, "—", Font, Theme.Muted, new RectangleF(x, cy - 12, 54, 24), true);
                return;
            }
            var ordered = OriginGroups.Build(preview.Main, preview.Sub, column, entries);
            float cell = Math.Min(62, available / entries.Length), height = cell * entries.Length;
            float y = cy - height / 2;
            Drawing.Box(g, new RectangleF(x, y, 54, height), Theme.InsetSurface, Theme.Border, 11);
            if (hoveringOrigin && affinityColumn == column)
            {
                for (int first = 0; first < ordered.Length; first++)
                {
                    if ((ordered[first].Mask & affinityMask) == 0)
                        continue;
                    int last = first;
                    while (last + 1 < ordered.Length && (ordered[last + 1].Mask & affinityMask) != 0)
                        last++;
                    Drawing.Box(g, new RectangleF(x + 1, y + first * cell + 1, 52, (last - first + 1) * cell - 2), Theme.AccentSurface, Theme.Accent, 10);
                    first = last;
                }
            }
            // Parallel source rails preserve shared membership without intersecting outlines.
            for (int origin = 0; origin < (preview.Sub < 0 ? 1 : 2); origin++)
            {
                int bit = 1 << origin, type = origin == 0 ? preview.Main : preview.Sub;
                using (var rail = new Pen(Model.Colors[type], 2) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    for (int first = 0; first < ordered.Length; first++)
                    {
                        if ((ordered[first].Mask & bit) == 0)
                            continue;
                        int last = first;
                        while (last + 1 < ordered.Length && (ordered[last + 1].Mask & bit) != 0)
                            last++;
                        float railX = x + 4 + origin * 4;
                        g.DrawLine(rail, railX, y + first * cell + 9, railX, y + (last + 1) * cell - 9);
                        first = last;
                    }
                }
            }
            using (var divider = new Pen(Theme.Border))
                for (int i = 1; i < ordered.Length; i++)
                    g.DrawLine(divider, x + 15, y + i * cell, x + 43, y + i * cell);
            using (var small = UiFonts.Create("Segoe UI", 8.5f, FontStyle.Bold))
                for (int i = 0; i < entries.Length; i++)
                {
                    affinityHits.Add(Tuple.Create(new RectangleF(x, y + i * cell, 54, cell), ordered[i].Mask, column));
                    float icon = Math.Min(30, cell - 21);
                    VectorIcon.Draw(g, ordered[i].Type, new RectangleF(x + 4 + (54 - icon) / 2, y + i * cell + 5, icon, icon));
                    Drawing.Text(g, Model.Factor(values[column * 9 + ordered[i].Type]), small, Theme.TextColor, new RectangleF(x + 9, y + (i + 1) * cell - 18, 43, 16), true);
                }
        }
        // Inputs: empty selection and viewport. Output: centered illustrative invitation, with no invented matchup values. Fallback: none. Raises: none.
        private void PaintEmptyState(Graphics g)
        {
            float cx = canvas.Width / 2f, cy = canvas.Height / 2f - 54;
            using (var orbit = new Pen(Theme.Border, 1))
            {
                g.DrawEllipse(orbit, cx - 69, cy - 69, 138, 138);
                g.DrawEllipse(orbit, cx - 45, cy - 45, 90, 90);
            }
            string[] order = { "Dark", "Earth", "Fire", "Grass", "Ice", "Light", "Lightning", "Water", "Wind" };
            for (int i = 0; i < order.Length; i++)
            {
                double angle = (-90 + i * 40) * Math.PI / 180;
                float x = cx + (float)Math.Cos(angle) * 69, y = cy + (float)Math.Sin(angle) * 69;
                using (var halo = new SolidBrush(Theme.InsetSurface))
                    g.FillEllipse(halo, x - 17, y - 17, 34, 34);
                VectorIcon.Draw(g, Array.IndexOf(Model.Names, order[i]), new RectangleF(x - 11, y - 11, 22, 22));
            }
            Glyphs.Draw(g, "atk", new RectangleF(cx - 31, cy - 13, 26, 26), Theme.Muted);
            Glyphs.Draw(g, "def", new RectangleF(cx + 6, cy - 13, 26, 26), Theme.Muted);
            using (var heading = UiFonts.Create("Segoe UI", 17, FontStyle.Bold))
                Drawing.Text(g, "Explore type affinities", heading, Theme.TextColor, new RectangleF(0, cy + 99, canvas.Width, 34), true);
            using (var body = UiFonts.Create("Segoe UI", 10))
                Drawing.Text(g, "Hover to explore · Click to select", body, Theme.Muted, new RectangleF(0, cy + 139, canvas.Width, 25), true);
            using (var hint = UiFonts.Create("Segoe UI", 8.5f))
                Drawing.Text(g, "Choose a main type, then a second if needed.", hint, Theme.Muted, new RectangleF(0, cy + 174, canvas.Width, 22), true);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
