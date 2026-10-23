using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class SuggestionGraphic
    {
        internal static string Title(TeamSuggestions.Recommendation value)
        {
            return value.Counter ? "Suggested counter" : "Suggested coverage";
        }
        internal static Size Size(TeamSuggestions.Recommendation value)
        {
            return new Size(350, 174 + value.Answers.Length * 29);
        }
        internal static void Attach(ApplicationTips tips, Control control, TeamSuggestions.Recommendation value, string action)
        {
            string description = TeamTypeSelector.RecommendationTip(value, action);
            tips.SetToolTip(control, description);
            if (value != null)
                tips.SetGraphicTip(control, description, Size(value), (g, area) => Draw(g, area, value, action));
        }
        internal static void Draw(Graphics g, RectangleF area, TeamSuggestions.Recommendation value, string action)
        {
            float x = area.X, y = area.Y;
            using (var font = UiFonts.Create("Segoe UI", 8.5f))
            using (var bold = UiFonts.Create("Segoe UI", 9, FontStyle.Bold))
            {
                VectorIcon.Draw(g, value.Types.Main, new RectangleF(x, y, 24, 24));
                if (value.Types.Sub >= 0)
                    VectorIcon.Draw(g, value.Types.Sub, new RectangleF(x + 28, y, 24, 24));
                Drawing.Text(g, Title(value), bold, Theme.TextColor, new RectangleF(x + 62, y, 270, 24), false);
                Drawing.Text(g, "Projected answers before → after this addition", font, Theme.Muted, new RectangleF(x, y + 29, 345, 22), false);
                Glyphs.Draw(g, "atk", new RectangleF(x + 92, y + 56, 17, 17), Theme.Attack);
                Drawing.Text(g, value.Counter ? "Opponents" : "All types", font, Theme.Muted, new RectangleF(x, y + 53, 86, 22), false);
                Drawing.Text(g, "Attack", bold, Theme.Attack, new RectangleF(x + 115, y + 53, 90, 22), false);
                Glyphs.Draw(g, "def", new RectangleF(x + 221, y + 56, 17, 17), Theme.Defense);
                Drawing.Text(g, "Defense", bold, Theme.Defense, new RectangleF(x + 244, y + 53, 95, 22), false);
                y += 80;
                foreach (var answer in value.Answers)
                {
                    VectorIcon.Draw(g, answer.Main, new RectangleF(x + 2, y + 2, 21, 21));
                    if (answer.Sub >= 0)
                        VectorIcon.Draw(g, answer.Sub, new RectangleF(x + 27, y + 2, 21, 21));
                    Cell(g, new RectangleF(x + 88, y, 122, 25), font, answer.Attackers, answer.Dealt > 1.0001, answer.Dealt, false);
                    Cell(g, new RectangleF(x + 217, y, 122, 25), font, answer.Defenders, answer.Received < .9999, answer.Received, true);
                    y += 29;
                }
                Drawing.Text(g, "0 → 1  New coverage    ·    1 → 2  Backup", font, Theme.Muted, new RectangleF(x, y + 3, 345, 20), false);
                ShortcutText.Layout(g, action, font, Theme.TextColor, new RectangleF(x, y + 24, 345, 20), true);
                Drawing.Text(g, "Before: " + value.AppliedAllies + " applied + " + value.EarlierProposals.Length + " earlier proposals", font, Theme.Muted, new RectangleF(x, y + 44, 345, 20), false);
                float proposalX = x;
                foreach (var proposal in value.EarlierProposals)
                {
                    VectorIcon.Draw(g, proposal.Main, new RectangleF(proposalX, y + 67, 17, 17), true);
                    if (proposal.Sub >= 0)
                        VectorIcon.Draw(g, proposal.Sub, new RectangleF(proposalX + 19, y + 67, 17, 17), true);
                    proposalX += 48;
                }
            }
        }
        private static void Cell(Graphics g, RectangleF area, Font font, int before, bool useful, double factor, bool defense)
        {
            Drawing.Box(g, area, useful ? Theme.AccentSurface : Theme.Surface, useful ? Theme.Border : Color.Transparent, 4);
            Drawing.Text(g, before + " → " + (before + (useful ? 1 : 0)), font, useful ? Theme.TextColor : Theme.Muted, new RectangleF(area.X + 5, area.Y, 60, area.Height), false);
            Color ink = factor == 1 ? Theme.Muted : (defense ? factor < 1 : factor > 1) ? Theme.Favorable : Theme.Unfavorable;
            Drawing.Text(g, Model.Factor(factor), font, ink, new RectangleF(area.X + 65, area.Y, 55, area.Height), true);
        }
    }
}
