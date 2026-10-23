using System.Drawing;
using System.Drawing.Drawing2D;
namespace AniimoAffinities
{
    internal static class SuggestionHelpGraphic
    {
        internal static readonly Size Size = new Size(340, 354);
        internal static void Draw(Graphics g, RectangleF bounds)
        {
            var state = g.Save();
            g.TranslateTransform(bounds.X, bounds.Y);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var title = UiFonts.Create("Segoe UI", 10, FontStyle.Bold))
            using (var text = UiFonts.Create("Segoe UI", 9))
            {
                Glyphs.Draw(g, "suggest", new RectangleF(4, 3, 22, 22), Theme.AccentForeground);
                Drawing.Text(g, "Complementary suggestions", title, Theme.TextColor, new RectangleF(34, 0, 300, 28), false);
                string[] headings = { "Coverage", "Counter", "Build together" };
                string[] details = { "No known opponents: cover all nine types.\nThen add backup answers.", "Known opponents: target their elements.\nUnknown elements stay excluded.", "Mono / dual proposals follow slot order.\nEarlier proposals count, but stay unapplied." };
                string[] glyphs = { "def", "atk", "team" };
                for (int i = 0; i < 3; i++)
                {
                    int y = 36 + i * 70;
                    Drawing.Box(g, new RectangleF(0, y, 336, 64), Theme.InsetSurface, Theme.Border, 8);
                    Glyphs.Draw(g, glyphs[i], new RectangleF(10, y + 10, 20, 20), i == 1 ? Theme.Attack : Theme.Defense);
                    Drawing.Text(g, headings[i], title, Theme.TextColor, new RectangleF(40, y + 4, 288, 22), false);
                    System.Windows.Forms.TextRenderer.DrawText(g, details[i], text, new Rectangle(40, y + 27, 290, 34), Theme.Muted, System.Windows.Forms.TextFormatFlags.NoPadding | System.Windows.Forms.TextFormatFlags.WordBreak | System.Windows.Forms.TextFormatFlags.PreserveGraphicsTranslateTransform);
                }
                Drawing.Text(g, "Apply a row", title, Theme.TextColor, new RectangleF(0, 251, 106, 26), false);
                Key(g, title, "+", new RectangleF(110, 251, 26, 25));
                Drawing.Text(g, "or", text, Theme.Muted, new RectangleF(141, 251, 25, 25), true);
                Key(g, title, "Ctrl", new RectangleF(171, 251, 43, 25));
                Drawing.Text(g, "+", text, Theme.Muted, new RectangleF(216, 251, 18, 25), true);
                Key(g, title, "Enter", new RectangleF(237, 251, 55, 25));
                string[] legends = { "Weakness", "Resistance" };
                for (int i = 0; i < 2; i++)
                {
                    using (var pen = new Pen(i == 0 ? Theme.Unfavorable : Theme.Favorable, 2))
                        g.DrawEllipse(pen, 4 + i * 170, 293, 13, 13);
                    Drawing.Text(g, legends[i], text, Theme.TextColor, new RectangleF(26 + i * 170, 287, 135, 26), false);
                }
                Drawing.Text(g, "Elements only · no stats or moves", text, Theme.Muted, new RectangleF(0, 326, 336, 24), false);
            }
            g.Restore(state);
        }
        private static void Key(Graphics g, Font font, string text, RectangleF bounds)
        {
            Drawing.Box(g, bounds, Theme.Border, Theme.StrongBorder, 3);
            Drawing.Text(g, text, font, Theme.TextColor, bounds, true);
        }
    }
}
