using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SuggestionRadarDialog : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        protected override string CaptionGlyph
        {
            get
            {
                return "radar";
            }
        }
        internal readonly RadarRenderer.Series[] Series;
        internal readonly SettingsButton ApplyButton = new SettingsButton { Text = "Apply suggestion", Primary = true, AccessibleName = "Apply this suggested ally", TabIndex = 0 };
        private readonly int suggestedSlot;
        private readonly string source;
        internal int HighlightedSlot = -1;
        private RectangleF LegendBounds(int index, int width)
        {
            float cellWidth = (width - 32) / 3f;
            return new RectangleF(16 + index % 3 * cellWidth, 68 + index / 3 * 32, cellWidth - 6, 28);
        }
        internal int LegendSlotAt(Point point, int width)
        {
            for (int i = 0; i < Series.Length; i++)
                if (LegendBounds(i, width).Contains(point))
                    return Series[i].Slot;
            return -1;
        }
        internal SuggestionRadarDialog(TeamPlan plan, int slot, TeamSuggestions.Recommendation suggestion, int[] selected)
        {
            suggestedSlot = slot;
            source = SuggestionGraphic.Title(suggestion);
            var slots = selected.Where(i => i >= 0 && i < plan.Allies.Length && plan.Allies[i].Known).Distinct().ToArray();
            if (slots.Length == 0)
                slots = Enumerable.Range(0, plan.Allies.Length).Where(i => plan.Allies[i].Known).ToArray();
            Series = slots.Select(i => Snapshot(plan.Allies[i], i)).Concat(new[] { Snapshot(suggestion.Types, slot) }).ToArray();
            Text = "Suggested ally · Radar preview";
            ClientSize = new Size(900, 560);
            MinimumSize = new Size(740, 520);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            var chart = new DrawingPanel { Dock = DockStyle.Fill };
            chart.AccessibleName = "Suggested ally comparison";
            chart.AccessibleText = () => "Preview only. " + string.Join(". ", Series.Select(item =>
                (item.Slot == suggestedSlot ? source : item.Member.Title(item.Slot)) + ": "
                + Model.Names[item.Member.Main] + (item.Member.Sub >= 0 ? " and " + Model.Names[item.Member.Sub] : "")
                + ". Attack " + string.Join(", ", Model.Factors(item.Member.Main, item.Member.Sub).Take(9).Select(Model.Factor))
                + ". Defense " + string.Join(", ", Model.Factors(item.Member.Main, item.Member.Sub).Skip(9).Select(Model.Factor))));
            chart.Paint += (s, e) => PaintPreview(e.Graphics, chart.ClientSize);
            chart.MouseMove += (s, e) =>
            {
                int slotAtPointer = LegendSlotAt(e.Location, chart.Width);
                if (slotAtPointer != HighlightedSlot)
                {
                    HighlightedSlot = slotAtPointer;
                    chart.Invalidate();
                }
            };
            chart.MouseLeave += (s, e) => { HighlightedSlot = -1; chart.Invalidate(); };
            Controls.Add(chart);
            ApplyButton.SetBounds(ClientSize.Width - 184, 34, 166, 32);
            ApplyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ApplyButton.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            chart.Controls.Add(ApplyButton);
        }
        private static RadarRenderer.Series Snapshot(TeamMember member, int slot)
        {
            return new RadarRenderer.Series { Slot = slot, Member = new TeamMember { Name = member.Name, Main = member.Main, Sub = member.Sub } };
        }
        private void PaintPreview(Graphics g, Size size)
        {
            g.Clear(Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var font = UiFonts.Create("Segoe UI", 9))
            using (var title = UiFonts.Create("Segoe UI", 12, FontStyle.Bold))
            {
                Drawing.Text(g, "Hover a legend to highlight · Not applied · Esc to cancel", font, Theme.Muted, new RectangleF(16, 38, size.Width - 212, 24), false);
                float legendWidth = (size.Width - 32) / 3f;
                for (int i = 0; i < Series.Length; i++)
                {
                    var item = Series[i];
                    bool proposed = item.Slot == suggestedSlot;
                    float x = 16 + i % 3 * legendWidth, y = 68 + i / 3 * 32;
                    if (item.Slot == HighlightedSlot)
                        Drawing.Box(g, LegendBounds(i, size.Width), Theme.AccentSurface, Theme.ComparisonInk(item.Slot), 5);
                    using (var pen = new Pen(Theme.ComparisonInk(item.Slot), 3))
                        g.DrawLine(pen, x, y + 13, x + 22, y + 13);
                    VectorIcon.Draw(g, item.Member.Main, new RectangleF(x + 29, y + 2, 21, 21), proposed);
                    if (item.Member.Sub >= 0)
                        VectorIcon.Draw(g, item.Member.Sub, new RectangleF(x + 53, y + 2, 21, 21), proposed);
                    Drawing.Text(g, proposed ? source : item.Member.Title(item.Slot), font, Theme.TextColor, new RectangleF(x + 82, y, legendWidth - 88, 28), false);
                }
                float width = (size.Width - 36) / 2f;
                for (int column = 0; column < 2; column++)
                {
                    var box = new RectangleF(12 + column * (width + 12), 140, width, size.Height - 152);
                    Drawing.Box(g, box, Theme.Surface, Theme.Border, Theme.PanelRadius);
                    Glyphs.Draw(g, column == 0 ? "atk" : "def", new RectangleF(box.X + 14, box.Y + 12, 22, 22), Theme.TextColor);
                    Drawing.Text(g, column == 0 ? "Attack · higher is stronger" : "Defense · lower is safer", title, column == 0 ? Theme.Attack : Theme.Defense, new RectangleF(box.X + 45, box.Y + 10, width - 55, 28), false);
                    float radius = RadarRenderer.FitRadius(new SizeF(width, box.Height - 48));
                    RadarRenderer.DrawRadar(g, new PointF(box.X + width / 2, box.Y + 48 + (box.Height - 48) / 2), radius, column == 0, column == 1, new Selection(), 0, Series, HighlightedSlot);
                }
            }
        }
    }
}
