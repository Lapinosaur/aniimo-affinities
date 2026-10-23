using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class ThreatReportPanel : UserControl
    {
        private readonly DataGridView table = new SlimDataGridView();
        private readonly Label context = new Label();
        private readonly SettingsButton[] tabs;
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private TeamPlan plan;
        private bool enemy;
        private int mode;
        private sealed class Exchange
        {
            internal string Name;
            internal TeamMember Types;
            internal bool TypeOnly;
            internal double Received, Dealt;
        }
        private sealed class Entry
        {
            internal readonly List<Exchange> Exchanges = new List<Exchange>();
            internal TeamMember Types;
            internal TeamMember[] Threats;
            internal string Name, Detail, DamageProfile, WeakTip, ResistTip, ExchangeTip, Summary;
            internal int Disadvantaged, Trading;
            internal double Reply;
            internal int Common = -1;
            internal int Weak, Resistant, Total;
            internal double Maximum;
        }
        internal ThreatReportPanel()
        {
            BackColor = Theme.Background;
            Padding = new Padding(16);
            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Surface };
            tabs = new[] { "Single types", "Dual types", "Members" }.Select((text, index) => new SettingsButton
            {
                Text = text,
                AccessibleName = text + " threat report",
                Location = new Point(index * 150, 0),
                Size = new Size(150, 30),
                TabIndex = index,
                Surround = Theme.Surface
            }).ToArray();
            SettingsButton.Group(tabs);
            for (int i = 0; i < tabs.Length; i++)
            {
                int selected = i;
                tabs[i].Click += (s, e) => { mode = selected; Rebuild(); };
                bar.Controls.Add(tabs[i]);
            }
            var info = new SettingsButton { Glyph = "info", Quiet = true, AccessibleName = "Show matchup legend", Size = new Size(30, 30), Location = new Point(bar.Width - 30, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, Surround = Theme.Surface };
            info.Click += (s, e) => tips.ShowTip(info);
            tips.SetGraphicTip(info, "Matchup legend. Shield: received. Swords: dealt. Red: unfavorable. Green: favorable. Disadvantaged: take more, deal less. Pressured: take more, deal normal damage. Trade blows: both deal more.", new Size(280, 250), DrawLegend);
            bar.Controls.Add(info);
            context.BackColor = Theme.Surface;
            context.Dock = DockStyle.Bottom;
            context.Height = 22;
            context.ForeColor = Theme.Muted;
            context.TextAlign = ContentAlignment.MiddleLeft;
            table.Dock = DockStyle.Fill;
            table.ReadOnly = true;
            table.AllowUserToAddRows = table.AllowUserToDeleteRows = table.AllowUserToResizeRows = false;
            table.AllowUserToResizeColumns = false;
            table.RowHeadersVisible = false;
            table.MultiSelect = false;
            table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            table.AutoGenerateColumns = false;
            table.BackgroundColor = Theme.Surface;
            table.BorderStyle = BorderStyle.None;
            table.CellBorderStyle = DataGridViewCellBorderStyle.None;
            table.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            table.GridColor = Theme.Border;
            table.EnableHeadersVisualStyles = false;
            table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            table.ColumnHeadersHeight = 29;
            table.RowTemplate.Height = 44;
            table.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Surface, ForeColor = Theme.TextColor, SelectionBackColor = Theme.HoverSurface, SelectionForeColor = Theme.TextColor, Padding = new Padding(5, 0, 5, 0) };
            table.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.InsetSurface, ForeColor = Theme.Muted, SelectionBackColor = Theme.InsetSurface, Padding = new Padding(5, 0, 5, 0) };
            foreach (var column in new[] { new DataGridViewTextBoxColumn { Name = "type", HeaderText = "Threat", Width = 130 }, new DataGridViewTextBoxColumn { Name = "weak", HeaderText = "Weak allies", Width = 82 }, new DataGridViewTextBoxColumn { Name = "resist", HeaderText = "Resist", Width = 80 }, new DataGridViewTextBoxColumn { Name = "max", HeaderText = "Damage", Width = 70 }, new DataGridViewTextBoxColumn { Name = "detail", HeaderText = "Exposed members", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill } })
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (column.Name == "detail")
                    column.DefaultCellStyle.Padding = new Padding(5, 0, 16, 0);
                table.Columns.Add(column);
            }
            table.CellPainting += PaintCell;
            table.ShowCellToolTips = false;
            table.CellMouseEnter += (s, e) => ShowCellTip(e.ColumnIndex, e.RowIndex);

            table.Scroll += (s, e) => tips.SetRegionTip(table, "", Rectangle.Empty);
            table.CellEnter += (s, e) => { if (table.Focused) ShowCellTip(e.ColumnIndex, e.RowIndex); };
            table.AccessibleName = "Team threat table";
            Controls.Add(table);
            Controls.Add(bar);
            Controls.Add(context);
        }
        private void ShowCellTip(int column, int row)
        {
            if (column < 0)
                return;
            tips.SetHoverBubble(table, false);
            tips.SetScrollableGraphic(table, false);
            string text = row < 0 ? table.Columns[column].ToolTipText : table.Rows[row].Cells[column].ToolTipText;
            if (row >= 0 && table.Rows[row].Tag is Entry)
            {
                var entry = (Entry)table.Rows[row].Tag;
                var matches = entry.Exchanges.Where(item => column == 1 ? item.Received > 1.0001 : column == 2 ? item.Received < .9999 : true).ToArray();
                if (matches.Length > 0)
                {
                    int shown = matches.Length;
                    tips.SetHoverBubble(table, true);
                    tips.SetScrollableGraphic(table, true);
                    tips.SetRegionTip(table, text, table.GetCellDisplayRectangle(column, row, true), (g, bounds) =>
                    {
                        var clip = g.VisibleClipBounds;
                        int first = Math.Max(0, (int)Math.Floor((clip.Top - bounds.Y) / 82));
                        int last = Math.Min(shown, (int)Math.Ceiling((clip.Bottom - bounds.Y) / 82));
                        for (int i = first; i < last; i++)
                        {
                            var item = matches[i];
                            float top = bounds.Y + i * 82;
                            Drawing.Box(g, new RectangleF(bounds.X, top, 250, 74), Theme.Surface, Theme.Border, Theme.PanelRadius);
                            float y = top + 8;
                            using (var line = new Pen(Theme.Border))
                                g.DrawLine(line, bounds.X + 10, top + 39, bounds.X + 240, top + 39);
                            VectorIcon.Draw(g, item.Types.Main, new RectangleF(bounds.X + 12, y, 26, 26));
                            if (item.Types.Sub >= 0)
                                VectorIcon.Draw(g, item.Types.Sub, new RectangleF(bounds.X + 42, y, 26, 26));
                            if (!item.TypeOnly)
                                using (var title = UiFonts.Create("Segoe UI", 10, FontStyle.Bold))
                                    TextRenderer.DrawText(g, item.Name, title, Rectangle.Round(new RectangleF(bounds.X + 78, y + 3, 160, 24)), Theme.TextColor, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                            Glyphs.Draw(g, "def", new RectangleF(bounds.X + 12, y + 40, 14, 14), Theme.Muted);
                            Drawing.Text(g, Model.Factor(item.Received), Font, OutcomeInk(item.Received, true), new RectangleF(bounds.X + 33, y + 36, 67, 22), false);
                            Glyphs.Draw(g, "atk", new RectangleF(bounds.X + 126, y + 40, 14, 14), Theme.Muted);
                            Drawing.Text(g, Model.Factor(item.Dealt), Font, OutcomeInk(item.Dealt, false), new RectangleF(bounds.X + 147, y + 36, 67, 22), false);
                        }

                    }, new Size(250, shown * 82));
                    return;
                }
                tips.SetRegionTip(table, text, table.GetCellDisplayRectangle(column, row, true), (g, bounds) =>
                {
                    Glyphs.Draw(g, "def", new RectangleF(bounds.X + 8, bounds.Y + 9, 16, 16), Theme.Muted);
                    Drawing.Text(g, column == 2 ? "No resistant member" : "No vulnerable member", Font, Theme.Muted, new RectangleF(bounds.X + 34, bounds.Y + 3, 210, 28), false);
                }, new Size(250, 34));
                return;
            }
            tips.SetRegionTip(table, text, table.GetCellDisplayRectangle(column, row, true));
        }
        internal void DrawLegend(Graphics graphics, RectangleF bounds)
        {
            string[] titles = { "Disadvantaged", "Pressured", "Trade blows" };
            string[] replies = { "<1", "=1", ">1" };
            Color[] colors = { Theme.Unfavorable, Theme.Muted, Theme.Favorable };
            Drawing.Text(graphics, "Received     /     Dealt", Font, Theme.Muted, new RectangleF(bounds.X + 10, bounds.Y, 250, 22), false);
            for (int i = 0; i < 3; i++)
            {
                float x = bounds.X, y = bounds.Y + 28 + i * 72;
                Drawing.Box(graphics, new RectangleF(x, y, 274, 64), Theme.Surface, Theme.Border, Theme.PanelRadius);
                using (var title = UiFonts.Create("Segoe UI", 11, FontStyle.Bold))
                    Drawing.Text(graphics, titles[i], title, Theme.TextColor, new RectangleF(x + 14, y + 5, 250, 24), false);
                using (var line = new Pen(Theme.Border))
                    graphics.DrawLine(line, x + 14, y + 32, x + 260, y + 32);
                Glyphs.Draw(graphics, "def", new RectangleF(x + 14, y + 41, 14, 14), Theme.Muted);
                Drawing.Text(graphics, ">1", Font, Theme.Unfavorable, new RectangleF(x + 34, y + 36, 35, 24), true);
                Glyphs.Draw(graphics, "atk", new RectangleF(x + 95, y + 41, 14, 14), Theme.Muted);
                Drawing.Text(graphics, replies[i], Font, colors[i], new RectangleF(x + 115, y + 36, 35, 24), true);
                DrawTrend(graphics, new RectangleF(x + 226, y + 41, 14, 14), i - 1, colors[i]);
            }
        }
        internal void UpdateReport(TeamPlan value, bool opponents)
        {
            plan = value;
            enemy = opponents;
            Rebuild();
        }
        private static string TypeName(TeamMember member)
        {
            return Model.Names[member.Main] + (member.Sub < 0 ? "" : " + " + Model.Names[member.Sub]);
        }
        private void Rebuild()
        {
            if (plan == null)
                return;
            foreach (var tab in tabs)
            {
                tab.Primary = Array.IndexOf(tabs, tab) == mode;
                tab.Invalidate();
            }
            var members = (enemy ? plan.Opponents : plan.Allies).Select((member, index) => new { Member = member, Name = member.Title(index, enemy) }).Where(item => item.Member.Known).ToArray();
            var opposite = (enemy ? plan.Allies : plan.Opponents).Where(member => member.Known).ToArray();
            var candidates = TeamThreats.Candidates();
            var assessed = opposite.Length > 0 ? opposite : candidates;
            var memberTypes = members.Select(member => member.Member).ToArray();
            var memberNames = members.Select(member => member.Name).ToArray();
            Entry[] entries;
            if (mode == 2)
                entries = members.Select(member => BuildEntry(member.Member, member.Name, assessed, assessed.Select(TypeName).ToArray(), true))
                    .OrderByDescending(entry => entry.Weak).ThenByDescending(entry => entry.Maximum).ToArray();
            else
                entries = members.Length == 0 ? new Entry[0] : candidates.Where(target => (target.Sub >= 0) == (mode == 1))
                    .Select(target => BuildEntry(target, TypeName(target), memberTypes, memberNames, false))
                    .Where(entry => entry.Weak > 0).OrderBy(entry => entry.Resistant > 0).ThenByDescending(entry => entry.Weak).ThenByDescending(entry => entry.Maximum).ToArray();
            entries = entries.OrderByDescending(entry => entry.Disadvantaged).ThenByDescending(entry => entry.Weak - entry.Trading).ThenByDescending(entry => entry.Weak).ToArray();
            int threatCount = entries.Length;
            if (mode == 1)
                entries = GroupPairs(entries);
            table.Columns[0].Width = mode == 1 ? Math.Max(120, 58 + entries.Select(entry => entry.Threats.Length).DefaultIfEmpty(0).Max() * 22) : mode == 2 ? 190 : 100;
            table.Columns[1].Width = 82;
            table.Columns[2].Width = 78;
            table.Columns[3].Width = 100;
            table.Columns[0].HeaderText = mode == 2 ? "Member" : "Threat";
            table.Columns[0].ToolTipText = mode == 1 ? "Common type + each following icon: one pair per icon. Grouped pairs have identical incoming AND outgoing damage for every known member." : "";
            table.Columns[4].HeaderText = "Matchup";
            table.Columns[1].HeaderText = mode == 2 ? "Weak to" : "Weak allies";
            table.Rows.Clear();
            foreach (var entry in entries)
            {
                int row = table.Rows.Add(entry.Name, entry.Weak + "/" + entry.Total, entry.Resistant + "/" + entry.Total, Model.Factor(entry.Maximum), entry.Detail.Length == 0 ? "—" : entry.Detail);
                table.Rows[row].Tag = entry;
                foreach (DataGridViewCell cell in table.Rows[row].Cells)
                    cell.ToolTipText = entry.Name + "\n" + entry.Weak + "/" + entry.Total + " vulnerable · " + entry.Resistant + "/" + entry.Total + " resistant · max " + Model.Factor(entry.Maximum) + "\n" + entry.Detail;
                table.Rows[row].Cells[1].ToolTipText = entry.WeakTip;
                table.Rows[row].Cells[2].ToolTipText = entry.ResistTip;
                table.Rows[row].Cells[3].ToolTipText = entry.ExchangeTip;
                table.Rows[row].Cells[4].ToolTipText = "Left: received · Right: dealt\n\n" + entry.ExchangeTip;
                table.Rows[row].Cells[4].Value = entry.Summary;
            }
            context.Text = members.Length == 0 ? "Choose elements to see the report." : entries.Length == 0 ? "No super-effective threat." : mode == 2 ? (opposite.Length > 0 ? "Against known opposing members" : "Against all 45 type combinations") + " · Types only" : members.Length + " known members · " + threatCount + " threats" + (mode == 1 ? " · " + entries.Length + " groups" : "");
        }
        private static Entry BuildEntry(TeamMember types, string name, TeamMember[] others, string[] names, bool memberMode)
        {
            var entry = new Entry { Types = types, Name = name, Total = others.Length };
            var threats = new List<TeamMember>();
            var affected = new List<string>();
            var weak = new List<string>();
            var resistant = new List<string>();
            var exchanges = new List<string>();
            int pressured = 0;
            for (int i = 0; i < names.Length; i++)
            {
                var defender = memberMode ? types : others[i];
                var attacker = memberMode ? others[i] : types;
                double incoming = TeamPlan.Damage(attacker, defender), outgoing = TeamPlan.Damage(defender, attacker);
                entry.Exchanges.Add(new Exchange { Name = names[i], Types = memberMode ? attacker : defender, TypeOnly = memberMode, Received = incoming, Dealt = outgoing });
                entry.Maximum = Math.Max(entry.Maximum, incoming);
                string description = names[i] + "\nDamage received: " + Model.Factor(incoming) + "\nDamage dealt: " + Model.Factor(outgoing);
                if (incoming > 1.0001)
                {
                    entry.Weak++;
                    threats.Add(others[i]);
                    affected.Add(names[i]);
                    weak.Add(description);
                    entry.Reply = Math.Max(entry.Reply, outgoing);
                    if (outgoing < .9999)
                    {
                        entry.Disadvantaged++;
                    }
                    else if (outgoing > 1.0001)
                    {
                        entry.Trading++;
                    }
                    else
                    {
                        pressured++;
                    }
                }
                if (incoming < .9999)
                {
                    entry.Resistant++;
                    resistant.Add(description);
                }
                exchanges.Add(description);
            }
            entry.WeakTip = "Receive increased damage (shield >1)\n" + (weak.Count == 0 ? "None" : string.Join("\n\n", weak));
            entry.ResistTip = "Receive reduced damage (shield <1)\n" + (resistant.Count == 0 ? "None" : string.Join("\n\n", resistant));
            entry.ExchangeTip = string.Join("\n\n", exchanges);
            var labels = new List<string>();
            if (entry.Disadvantaged > 0)
                labels.Add(entry.Disadvantaged + " disadvantaged");
            if (pressured > 0)
                labels.Add(pressured + " pressured");
            if (entry.Trading > 0)
                labels.Add(entry.Trading + " trade blows");
            entry.Summary = labels.Count == 0 ? "No weakness" : string.Join(" · ", labels);
            if (weak.Count == 0)
                entry.Reply = entry.Exchanges.Count == 0 ? 1 : entry.Exchanges.Max(item => item.Dealt);
            entry.Threats = threats.ToArray();
            entry.Detail = memberMode ? string.Join(", ", affected) : TeamMember.JoinNames(affected.ToArray(), entry.Threats);
            entry.DamageProfile = string.Join("/", entry.Exchanges.Select(item => item.Received.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + item.Dealt.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            return entry;
        }
        private static Entry[] GroupPairs(Entry[] entries)
        {
            var remaining = entries.ToList();
            var result = new List<Entry>();
            while (remaining.Count > 0)
            {
                var first = remaining[0];
                var equivalent = remaining.Where(entry => entry.DamageProfile == first.DamageProfile).ToArray();
                int common = Enumerable.Range(0, Model.Names.Length).OrderByDescending(type => equivalent.Count(entry => entry.Types.Main == type || entry.Types.Sub == type)).First();
                var group = equivalent.Where(entry => entry.Types.Main == common || entry.Types.Sub == common).ToArray();
                var row = group[0];
                row.Common = common;
                row.Threats = group.Select(entry => entry.Types).ToArray();
                row.Name = string.Join("; ", group.Select(entry => TypeName(entry.Types)));
                result.Add(row);
                foreach (var entry in group)
                    remaining.Remove(entry);
            }
            return result.ToArray();
        }
        private void PaintCell(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;
            if (e.RowIndex < 0)
            {
                using (var brush = new SolidBrush(Theme.Surface))
                    e.Graphics.FillRectangle(brush, e.CellBounds);
                if (e.ColumnIndex == 1 || e.ColumnIndex == 2)
                {
                    Color ink = e.ColumnIndex == 1 ? Theme.Unfavorable : Theme.Favorable;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    Drawing.Box(e.Graphics, new RectangleF(e.CellBounds.X + 7, e.CellBounds.Y + 6, 18, 18), Color.FromArgb(28, ink), Color.Transparent, 5);
                    Glyphs.Draw(e.Graphics, "def", new RectangleF(e.CellBounds.X + 9, e.CellBounds.Y + 8, 14, 14), ink);
                    TextRenderer.DrawText(e.Graphics, e.ColumnIndex == 1 ? "Weak" : "Resist", Font, new Rectangle(e.CellBounds.X + 29, e.CellBounds.Y, e.CellBounds.Width - 31, e.CellBounds.Height), ink, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
                else
                    TextRenderer.DrawText(e.Graphics, Convert.ToString(e.FormattedValue), Font, Rectangle.Inflate(e.CellBounds, -8, 0), Theme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                e.Handled = true;
                return;
            }
            var entry = (Entry)table.Rows[e.RowIndex].Tag;
            if (entry == null)
                return;
            using (var brush = new SolidBrush(Theme.Surface))
                e.Graphics.FillRectangle(brush, e.CellBounds);
            var state = e.Graphics.Save();
            e.Graphics.SetClip(Rectangle.Intersect(e.ClipBounds, e.CellBounds));
            var rowBounds = table.GetRowDisplayRectangle(e.RowIndex, false);
            var card = new RectangleF(1, rowBounds.Y + 3, table.ClientSize.Width - 17, rowBounds.Height - 6);
            bool selected = table.Rows[e.RowIndex].Selected && table.ContainsFocus;
            if (selected)
                Drawing.Box(e.Graphics, card, Theme.HoverSurface, Theme.Accent, 9);
            else
                using (var line = new Pen(Theme.Border))
                    e.Graphics.DrawLine(line, 8, rowBounds.Bottom - 1, table.ClientSize.Width - 20, rowBounds.Bottom - 1);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (e.ColumnIndex == 4)
            {
                int category = entry.Disadvantaged > 0 ? -1 : entry.Weak > entry.Trading ? 0 : entry.Trading > 0 ? 1 : 2;
                var involved = entry.Exchanges.Where(item => item.Received > 1.0001 && (category < 0 ? item.Dealt < .9999 : category == 0 ? item.Dealt >= .9999 && item.Dealt <= 1.0001 : item.Dealt > 1.0001)).ToArray();
                string label = category < 0 ? "Disadvantage" : category == 0 ? "Under pressure" : category == 1 ? "Trade blows" : "No weakness";
                if (involved.Length > 1)
                    label = involved.Length + " · " + label;
                Color ink = category < 0 ? Theme.Unfavorable : Theme.TextColor;
                var titleBounds = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 3, e.CellBounds.Width - 26, 19);
                TextRenderer.DrawText(e.Graphics, label, Font, titleBounds, ink, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                if (mode == 2)
                {
                    int visible = Math.Min(involved.Length, Math.Max(1, (e.CellBounds.Width - 38) / 42));
                    for (int i = 0; i < visible; i++)
                    {
                        float x = e.CellBounds.X + 8 + i * 42;
                        VectorIcon.Draw(e.Graphics, involved[i].Types.Main, new RectangleF(x, e.CellBounds.Y + 23, 16, 16));
                        if (involved[i].Types.Sub >= 0)
                            VectorIcon.Draw(e.Graphics, involved[i].Types.Sub, new RectangleF(x + 18, e.CellBounds.Y + 23, 16, 16));
                    }
                    if (involved.Length > visible)
                        Drawing.Text(e.Graphics, "+" + (involved.Length - visible), Font, Theme.Muted, new RectangleF(e.CellBounds.X + 8 + visible * 42, e.CellBounds.Y + 21, 26, 20), false);
                }
                else
                {
                    int available = e.CellBounds.Width - 26;
                    int visible = Math.Min(involved.Length, Math.Max(1, (available - (involved.Length > 1 ? 26 : 0)) / 92));
                    int itemWidth = (available - (involved.Length > visible ? 26 : 0)) / Math.Max(1, visible);
                    for (int i = 0; i < visible; i++)
                    {
                        var ally = involved[i];
                        int x = e.CellBounds.X + 8 + i * itemWidth;
                        VectorIcon.Draw(e.Graphics, ally.Types.Main, new RectangleF(x, e.CellBounds.Y + 23, 16, 16));
                        int inset = ally.Types.Sub >= 0 ? 38 : 20;
                        if (ally.Types.Sub >= 0)
                            VectorIcon.Draw(e.Graphics, ally.Types.Sub, new RectangleF(x + 18, e.CellBounds.Y + 23, 16, 16));
                        TextRenderer.DrawText(e.Graphics, ally.Name, Font, new Rectangle(x + inset, e.CellBounds.Y + 22, Math.Max(1, itemWidth - inset - 4), 19), Theme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                    }
                    if (involved.Length > visible)
                        Drawing.Text(e.Graphics, "+" + (involved.Length - visible), Font, Theme.Muted, new RectangleF(e.CellBounds.X + 8 + visible * itemWidth, e.CellBounds.Y + 21, 26, 20), false);
                }
            }
            else if (e.ColumnIndex == 0 && mode == 2)
            {
                float y = e.CellBounds.Y + (e.CellBounds.Height - 22) / 2f;
                VectorIcon.Draw(e.Graphics, entry.Types.Main, new RectangleF(e.CellBounds.X + 8, y, 22, 22));
                if (entry.Types.Sub >= 0)
                    VectorIcon.Draw(e.Graphics, entry.Types.Sub, new RectangleF(e.CellBounds.X + 34, y, 22, 22));
                var textBounds = new Rectangle(e.CellBounds.X + 64, e.CellBounds.Y, e.CellBounds.Width - 72, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, entry.Name, Font, textBounds, Theme.TextColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
            else if (e.ColumnIndex == 0)
            {
                if (entry.Common >= 0)
                {
                    VectorIcon.Draw(e.Graphics, entry.Common, new RectangleF(e.CellBounds.X + 8, e.CellBounds.Y + 10, 24, 24));
                    Drawing.Text(e.Graphics, "+", Font, Theme.Muted, new RectangleF(e.CellBounds.X + 32, e.CellBounds.Y, 20, 44), true);
                    for (int i = 0; i < entry.Threats.Length; i++)
                    {
                        var pair = entry.Threats[i];
                        VectorIcon.Draw(e.Graphics, pair.Main == entry.Common ? pair.Sub : pair.Main, new RectangleF(e.CellBounds.X + 54 + i * 22, e.CellBounds.Y + 12, 20, 20));
                    }
                }
                else
                    VectorIcon.Draw(e.Graphics, entry.Types.Main, new RectangleF(e.CellBounds.X + 12, e.CellBounds.Y + 10, 24, 24));
            }
            else
            {
                Color ink = e.ColumnIndex == 1 ? (entry.Weak > 0 ? Theme.Unfavorable : Theme.Muted) : e.ColumnIndex == 2 ? (entry.Resistant > 0 ? Theme.Favorable : Theme.Muted) : OutcomeInk(entry.Maximum, true);
                var badge = new RectangleF(e.CellBounds.X + 6, e.CellBounds.Y + 10, e.CellBounds.Width - 12, 24);
                string label = e.ColumnIndex == 1 ? entry.Weak.ToString() : e.ColumnIndex == 2 ? entry.Resistant.ToString() : Convert.ToString(e.FormattedValue);
                if (e.ColumnIndex == 3)
                {
                    Glyphs.Draw(e.Graphics, "def", new RectangleF(badge.X, e.CellBounds.Y + 3, 18, 18), Theme.TextColor);
                    Glyphs.Draw(e.Graphics, "atk", new RectangleF(badge.X, e.CellBounds.Y + 24, 18, 18), Theme.TextColor);
                    using (var multiplier = UiFonts.Create("Segoe UI", 11, FontStyle.Bold))
                    {
                        Drawing.Text(e.Graphics, Model.Factor(entry.Maximum), multiplier, ink, new RectangleF(badge.X + 24, e.CellBounds.Y + 1, badge.Width - 24, 21), false);
                        Drawing.Text(e.Graphics, Model.Factor(entry.Reply), multiplier, OutcomeInk(entry.Reply, false), new RectangleF(badge.X + 24, e.CellBounds.Y + 22, badge.Width - 24, 21), false);
                    }
                }
                else
                {
                    var countBounds = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - 42) / 2f, e.CellBounds.Y + 8, 42, 28);
                    Drawing.Box(e.Graphics, countBounds, Color.FromArgb(label == "0" ? 8 : 24, ink), Color.Transparent, 8);
                    using (var number = UiFonts.Create("Segoe UI", 11, FontStyle.Bold))
                        Drawing.Text(e.Graphics, label, number, ink, countBounds, true);
                }
            }
            e.Graphics.Restore(state);
            e.Handled = true;
        }
        private static Color OutcomeInk(double damage, bool incoming)
        {
            if (damage > .9999 && damage < 1.0001)
                return Theme.Muted;
            bool favorable = incoming ? damage < 1 : damage > 1;
            return favorable ? Theme.Favorable : Theme.Unfavorable;
        }
        private static void DrawTrend(Graphics g, RectangleF box, int direction, Color color)
        {
            float side = Math.Min(box.Width, box.Height);
            box = new RectangleF(box.X + (box.Width - side) / 2, box.Y + (box.Height - side) / 2, side, side);
            float startY = direction > 0 ? box.Bottom : direction < 0 ? box.Top : box.Top + box.Height / 2;
            float endY = direction > 0 ? box.Top : direction < 0 ? box.Bottom : startY;
            using (var pen = new Pen(color, 1.6f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
            {
                g.DrawLine(pen, box.Left, startY, box.Right, endY);
                if (direction == 0)
                    return;
                g.DrawLine(pen, box.Right - 4, endY, box.Right, endY);
                g.DrawLine(pen, box.Right, endY, box.Right, endY + (direction < 0 ? -4 : 4));
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Drawing.Box(e.Graphics, new RectangleF(1, 1, Width - 3, Height - 3), Theme.Surface, Theme.Border, Theme.PanelRadius);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
