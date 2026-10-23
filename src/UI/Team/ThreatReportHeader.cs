using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class ThreatReportHeader : SettingsButton
    {
        private bool hover;
        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            base.OnMouseEnter(e);
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            base.OnMouseLeave(e);
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            float zoom = WindowZoom.Factor(this);
            var g = e.Graphics;
            g.ScaleTransform(zoom, zoom);
            float width = Width / zoom, height = Height / zoom;
            g.Clear(Theme.Background);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Drawing.Box(g, new RectangleF(1, 1, width - 3, height - 3), hover || InteractionPressed ? Theme.HoverSurface : Theme.Surface, Focused && ShowFocusCues ? Theme.Accent : Theme.Border, Theme.PanelRadius);
            Drawing.Box(g, new RectangleF(16, 16, 32, 32), Theme.InsetSurface, Theme.Border, 9);
            Glyphs.Draw(g, "def", new RectangleF(22, 22, 20, 20), Theme.TextColor);
            using (var title = UiFonts.Create("Segoe UI", 13, FontStyle.Bold))
                Drawing.Text(g, "Team analysis", title, Theme.TextColor, new RectangleF(60, 9, width - 105, 27), false);
            Drawing.Text(g, "Threats, resistances & replies", Font, Theme.Muted, new RectangleF(60, 35, width - 105, 20), false);
            Drawing.Text(g, Text.Contains("▴") ? "−" : "+", Font, Theme.Muted, new RectangleF(width - 40, 16, 24, 32), true);
            FinishPaint(g, (int)width, (int)height, false);
        }
    }
}
