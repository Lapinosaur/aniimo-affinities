using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TipBubble : Form
    {
        private string label; private bool below; private float arrowX;
        private const TextFormatFlags TextFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter;
        internal TipBubble()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Surface;
            DoubleBuffered = true;
            Font = UiFonts.Create("Segoe UI", 8.25f, FontStyle.Bold);
        }
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ExStyle |= 0x08000000 | 0x80 | 0x20;
                return p;
            }
        }
        // Inputs: hovered control, text. Output: clamped non-activating bubble with arrow pointing at anchor. Fallback: flip above when bottom lacks room. Raises: none.
        internal void Present(Control anchor, string text)
        {
            BackColor = Theme.Surface;
            label = text;
            var bounds = anchor.RectangleToScreen(anchor.ClientRectangle);
            var screen = Screen.FromControl(anchor).WorkingArea;
            // Measure and paint with the same renderer, width and wrapping rules.
            int maxTextWidth = Math.Max(40, Math.Min(264, screen.Width - 34));
            var measured = TextRenderer.MeasureText(text, Font, new Size(maxTextWidth, int.MaxValue), TextFlags);
            int textWidth = Math.Min(maxTextWidth, measured.Width + 2);
            measured = TextRenderer.MeasureText(text, Font, new Size(textWidth, int.MaxValue), TextFlags);
            Size = new Size(textWidth + 20, measured.Height + 22);
            below = bounds.Bottom + 7 + Height <= screen.Bottom - 7;
            int left = Math.Max(screen.Left + 7, Math.Min(bounds.Left + (bounds.Width - Width) / 2, screen.Right - Width - 7));
            int top = below ? bounds.Bottom + 7 : Math.Max(screen.Top + 7, bounds.Top - Height - 7);
            arrowX = Math.Max(12, Math.Min(Width - 14, bounds.Left + bounds.Width / 2f - left));
            Location = new Point(left, top);
            // Clip the window to its silhouette instead of color-keying antialiased pixels.
            var body = new RectangleF(1, below ? 6 : 1, Width - 4, Height - 10);
            using (var outline = Drawing.Round(body, 6))
            {
                var shape = new Region(outline);
                using (var shadow = Drawing.Round(new RectangleF(body.X + 2, body.Y + 2, body.Width, body.Height), 6))
                    shape.Union(shadow);
                float edge = below ? body.Top : body.Bottom;
                using (var arrow = new GraphicsPath())
                {
                    arrow.AddPolygon(new[] { new PointF(arrowX - 5, edge), new PointF(arrowX, below ? edge - 5 : edge + 5), new PointF(arrowX + 5, edge) });
                    shape.Union(arrow);
                }
                var previous = Region;
                Region = shape;
                if (previous != null)
                    previous.Dispose();
            }
            Show(anchor.FindForm());
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var body = new RectangleF(1, below ? 6 : 1, Width - 4, Height - 10);
            using (var shadow = Drawing.Round(new RectangleF(body.X + 2, body.Y + 2, body.Width, body.Height), 6))
            using (var brush = new SolidBrush(Theme.TooltipShadow))
                g.FillPath(brush, shadow);
            Drawing.Box(g, body, Theme.Surface, Theme.StrongBorder, 6);
            float edge = below ? body.Top : body.Bottom, tip = below ? edge - 5 : edge + 5;
            var points = new[] { new PointF(arrowX - 5, edge), new PointF(arrowX, tip), new PointF(arrowX + 5, edge) };
            using (var fill = new SolidBrush(Theme.Surface))
                g.FillPolygon(fill, points);
            using (var erase = new Pen(Theme.Surface, 2))
                g.DrawLine(erase, arrowX - 4, edge, arrowX + 4, edge);
            using (var border = new Pen(Theme.StrongBorder))
                g.DrawLines(border, points);
            TextRenderer.DrawText(g, label, Font, Rectangle.Round(new RectangleF(body.X + 8, body.Y + 6, body.Width - 16, body.Height - 12)), Theme.TextColor, TextFlags);
        }
    }
}
