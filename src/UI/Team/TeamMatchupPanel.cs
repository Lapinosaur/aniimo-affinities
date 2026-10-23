using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TeamMatchupPanel : DrawingPanel
    {
        private readonly TeamPlan plan;
        internal TeamMatchupPanel(TeamPlan value)
        {
            plan = value;
            AccessibleName = "Team matchup comparison";
            AccessibleRole = AccessibleRole.Table;
            AccessibleText = () => string.Join("; ", plan.Allies.SelectMany((ally, i) => plan.Opponents.Select((opponent, j) => ally.Title(i) + " against " + opponent.Title(j, true) + ": " + (!ally.Known || !opponent.Known ? "not set" : "attack " + Model.Factor(TeamPlan.Damage(ally, opponent)) + ", damage received " + Model.Factor(TeamPlan.Damage(opponent, ally))))).ToArray());
        }
        internal void Configure()
        {
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var font = UiFonts.Create("Segoe UI", 8))
                {
                    Drawing.Text(g, "Your team ↓", font, Theme.Muted, new RectangleF(0, 0, 108, 30), false);
                    float cell = (Width - 108) / 6f;
                    for (int j = 0; j < 6; j++)
                    {
                        var opponent = plan.Opponents[j];
                        DrawName(g, opponent.Title(j, true), font, Theme.Muted, new RectangleF(108 + j * cell, 0, cell - 4, 20), true);
                        if (!opponent.Known)
                            DrawName(g, "Not set", font, Theme.Muted, new RectangleF(108 + j * cell, 20, cell - 4, 20), true);
                        DrawElements(g, opponent, 108 + j * cell + (cell - (opponent.Sub >= 0 ? 38 : 18)) / 2, 21);
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        var ally = plan.Allies[i];
                        DrawName(g, ally.Title(i), font, Theme.TextColor, new RectangleF(0, 44 + i * 44, 104, 20), false);
                        DrawElements(g, ally, 0, 65 + i * 44);
                        for (int j = 0; j < 6; j++)
                        {
                            var opponent = plan.Opponents[j];
                            var bounds = new RectangleF(108 + j * cell, 44 + i * 44, cell - 4, 40);
                            Drawing.Box(g, bounds, Theme.Surface, Theme.Border, 5);
                            if (!ally.Known || !opponent.Known)
                            {
                                Drawing.Text(g, "—", font, Theme.Muted, bounds, true);
                                continue;
                            }
                            double dealt = TeamPlan.Damage(ally, opponent), received = TeamPlan.Damage(opponent, ally);
                            // Both halves use the player's perspective: higher attack and lower incoming damage are advantages.
                            var state = g.Save();
                            using (var outline = Drawing.Round(bounds, 5))
                            {
                                g.SetClip(outline, CombineMode.Intersect);
                                using (var attackFill = new SolidBrush(Theme.MultiplierFill(dealt)))
                                    g.FillRectangle(attackFill, bounds.X, bounds.Y, bounds.Width, 20);
                                using (var defenseFill = new SolidBrush(Theme.MultiplierFill(received == 0 ? double.PositiveInfinity : 1 / received)))
                                    g.FillRectangle(defenseFill, bounds.X, bounds.Y + 20, bounds.Width, 20);
                                g.Restore(state);
                                using (var border = new Pen(Theme.Border))
                                    g.DrawPath(border, outline);
                            }
                            Glyphs.Draw(g, "atk", new RectangleF(bounds.X + 6, bounds.Y + 4, 12, 12), Theme.Attack);
                            Glyphs.Draw(g, "def", new RectangleF(bounds.X + 6, bounds.Y + 24, 12, 12), Theme.Defense);
                            Drawing.Text(g, Model.Factor(dealt), font, Theme.MatchupInk(dealt, false), new RectangleF(bounds.X + 23, bounds.Y, bounds.Width - 26, 20), true);
                            Drawing.Text(g, Model.Factor(received), font, Theme.MatchupInk(received, true), new RectangleF(bounds.X + 23, bounds.Y + 20, bounds.Width - 26, 20), true);
                        }
                    }
                }
            };
        }
        private static void DrawElements(Graphics g, TeamMember member, float x, float y)
        {
            if (!member.Known)
                return;
            VectorIcon.Draw(g, member.Main, new RectangleF(x, y, 18, 18));
            if (member.Sub >= 0)
                VectorIcon.Draw(g, member.Sub, new RectangleF(x + 20, y, 18, 18));
        }
        private static void DrawName(Graphics g, string name, Font font, Color color, RectangleF bounds, bool center)
        {
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat { Alignment = center ? StringAlignment.Center : StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(name, font, brush, bounds, format);
        }
    }
}
