using System.Drawing;
namespace AniimoAffinities
{
    internal static class SupportBubbleContent
    {
        internal static void Draw(Graphics g, RectangleF body, Image qr)
        {
            using (var title = UiFonts.Create("Segoe UI", 10, FontStyle.Bold))
            using (var heading = UiFonts.Create("Segoe UI", 9, FontStyle.Bold))
            using (var text = UiFonts.Create("Segoe UI", 8))
            {
                Drawing.Text(g, Language.T("Support Lapinosaur"), title, Theme.TextColor, new RectangleF(body.X + 14, body.Y + 10, body.Width - 80, 24), false);
                var icon = new RectangleF(body.X + 14, body.Y + 48, 36, 36);
                Drawing.Box(g, icon, Theme.AccentSurface, Theme.Border, 9);
                Glyphs.Draw(g, "phone", new RectangleF(icon.X + 6, icon.Y + 6, 24, 24), Theme.AccentForeground);
                Drawing.Text(g, Language.T("Buy me a carrot"), heading, Theme.TextColor, new RectangleF(body.X + 14, body.Y + 91, 160, 24), false);
                Drawing.Text(g, Language.T("Scan with your phone\nor click the QR code."), text, Theme.Muted, new RectangleF(body.X + 14, body.Y + 118, 166, 40), false);
                using (var pen = new Pen(Theme.Border))
                    g.DrawLine(pen, body.X + 14, body.Y + 174, body.Right - 14, body.Y + 174);
            }
        }
    }
}
