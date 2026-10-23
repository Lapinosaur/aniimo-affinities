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
        private Rectangle anchorBounds;
        private int arrowSide;
        private float arrowY;
        private RectangleF Body
        {
            get
            {
                return new RectangleF(arrowSide == 1 ? 6 : 1, below && arrowSide == 0 ? 6 : 1, Width - 10, Height - 10);
            }
        }
        internal int ArrowSide
        {
            get
            {
                return arrowSide;
            }
        }
        private PointF[] ArrowPoints(RectangleF body)
        {
            if (arrowSide != 0)
            {
                float x = arrowSide == 1 ? body.Left : body.Right;
                return new[] { new PointF(x, arrowY - 5), new PointF(x + (arrowSide == 1 ? -5 : 5), arrowY), new PointF(x, arrowY + 5) };
            }
            float edge = below ? body.Top : body.Bottom;
            return new[] { new PointF(arrowX - 5, edge), new PointF(arrowX, edge + (below ? -5 : 5)), new PointF(arrowX + 5, edge) };
        }
        internal void Relocate(Rectangle destination)
        {
            Bounds = destination;
            arrowSide = Left >= anchorBounds.Right ? 1 : Right <= anchorBounds.Left ? 2 : 0;
            below = Top >= anchorBounds.Bottom;
            arrowX = Math.Max(12, Math.Min(Width - 18, anchorBounds.Left + anchorBounds.Width / 2f - Left));
            arrowY = Math.Max(12, Math.Min(Height - 18, anchorBounds.Top + anchorBounds.Height / 2f - Top));
            UpdateShape();
            Invalidate();
        }
        private Image illustration;
        private Action<Graphics, RectangleF> graphicPainter;
        private const int QrSize = 111;
        private sealed class TipRow
        {
            internal string Text;
            internal int Main = -1, Sub = -1;
            internal int Height;
            internal double Multiplier = double.NaN;
        }
        private readonly List<TipRow> rows = new List<TipRow>();
        private TextFormatFlags TextFlags
        {
            get
            {
                return TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | (label != null && label.Contains("\n") ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter);
            }
        }
        internal TipBubble()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Surface;
            DoubleBuffered = true;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 8.25f, FontStyle.Bold));
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
        internal void Present(Control anchor, string text, Rectangle? visualBounds = null, Image image = null, Action<Graphics, RectangleF> paint = null, Size? graphicSize = null)
        {
            graphicPainter = paint;
            illustration = image;
            BackColor = Theme.Surface;
            label = text;
            rows.Clear();
            if (text.Contains("[types:"))
                foreach (string line in text.Split('\n'))
                {
                    var row = new TipRow { Text = line };
                    int end = line.IndexOf(']');
                    if (line.StartsWith("[types:") && end > 7)
                    {
                        var types = line.Substring(7, end - 7).Split(',');
                        int main, sub;
                        if (types.Length == 2 && int.TryParse(types[0], out main) && int.TryParse(types[1], out sub) && main >= 0 && main < 9 && sub >= -1 && sub < 9)
                        {
                            row.Main = main;
                            row.Sub = sub;
                            row.Text = line.Substring(end + 1);
                            int multiplierStart = row.Text.LastIndexOf('×');
                            double multiplier;
                            if (multiplierStart >= 0 && double.TryParse(row.Text.Substring(multiplierStart + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out multiplier))
                            {
                                row.Multiplier = multiplier;
                                row.Text = row.Text.Substring(0, multiplierStart).TrimEnd();
                            }
                        }
                    }
                    rows.Add(row);
                }
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 8.5f, text.Contains("\n") ? FontStyle.Regular : FontStyle.Bold));
            var bounds = anchor.RectangleToScreen(visualBounds ?? anchor.ClientRectangle);
            anchorBounds = bounds;
            var screen = Screen.FromControl(anchor).WorkingArea;
            // Measure and paint with the same renderer, width and wrapping rules.
            int maxTextWidth = Math.Max(40, Math.Min(264, screen.Width - 34));
            var measured = TextRenderer.MeasureText(text, Font, new Size(maxTextWidth, int.MaxValue), TextFlags);
            int textWidth = Math.Min(maxTextWidth, measured.Width + 2);
            measured = TextRenderer.MeasureText(text, Font, new Size(textWidth, int.MaxValue), TextFlags);
            Size = new Size(textWidth + 20, measured.Height + 22);
            if (rows.Count > 0)
            {
                int width = 0, height = 0;
                foreach (var row in rows)
                {
                    int inset = row.Main < 0 ? 0 : 42;
                    int suffix = double.IsNaN(row.Multiplier) ? 0 : 60;
                    var size = TextRenderer.MeasureText(row.Text.Length == 0 ? " " : row.Text, Font, new Size(maxTextWidth - inset - suffix, int.MaxValue), TextFlags);
                    row.Height = Math.Max(row.Main < 0 ? 0 : 24, size.Height + 3);
                    width = Math.Max(width, size.Width + inset + suffix);
                    height += row.Height;
                }
                Size = new Size(Math.Min(maxTextWidth, width) + 24, height + 24);
            }
            if (illustration != null)
                Size = new Size(Math.Max(QrSize + 24, Width), Height + QrSize + 12);
            if (graphicPainter != null && graphicSize.HasValue)
                Size = new Size(graphicSize.Value.Width + 24, graphicSize.Value.Height + 24);
            Width += 6; // Reserve the side-arrow gutter without reducing the measured text area.
            below = bounds.Bottom + 7 + Height <= screen.Bottom - 7;
            int left = Math.Max(screen.Left + 7, Math.Min(bounds.Left + (bounds.Width - Width) / 2, screen.Right - Width - 7));
            int top = below ? bounds.Bottom + 7 : Math.Max(screen.Top + 7, bounds.Top - Height - 7);
            arrowX = Math.Max(12, Math.Min(Width - 14, bounds.Left + bounds.Width / 2f - left));
            Location = new Point(left, top);
            Relocate(Bounds);
            Show(anchor.FindForm());
            Invalidate();
        }
        private void UpdateShape()
        {
            // Clip the window to its silhouette instead of color-keying antialiased pixels.
            var body = Body;
            using (var outline = Drawing.Round(body, 6))
            {
                var shape = new Region(outline);
                using (var shadow = Drawing.Round(new RectangleF(body.X + 2, body.Y + 2, body.Width, body.Height), 6))
                    shape.Union(shadow);
                using (var arrow = new GraphicsPath())
                {
                    arrow.AddPolygon(ArrowPoints(body));
                    shape.Union(arrow);
                }
                var previous = Region;
                Region = shape;
                if (previous != null)
                    previous.Dispose();
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var body = Body;
            using (var shadow = Drawing.Round(new RectangleF(body.X + 2, body.Y + 2, body.Width, body.Height), 6))
            using (var brush = new SolidBrush(Theme.TooltipShadow))
                g.FillPath(brush, shadow);
            Drawing.Box(g, body, Theme.Surface, Theme.StrongBorder, 6);
            var points = ArrowPoints(body);
            using (var fill = new SolidBrush(Theme.Surface))
                g.FillPolygon(fill, points);
            using (var erase = new Pen(Theme.Surface, 2))
                g.DrawLine(erase, points[0], points[2]);
            using (var border = new Pen(Theme.StrongBorder))
                g.DrawLines(border, points);
            if (graphicPainter != null)
            {
                graphicPainter(g, new RectangleF(body.X + 8, body.Y + 6, body.Width - 16, body.Height - 12));
                return;
            }
            if (rows.Count == 0)
                TextRenderer.DrawText(g, label, Font, Rectangle.Round(new RectangleF(body.X + 8, body.Y + 6, body.Width - 16, body.Height - 12 - (illustration == null ? 0 : QrSize + 12))), Theme.TextColor, TextFlags);
            else
            {
                float y = body.Y + 6;
                foreach (var row in rows)
                {
                    int inset = row.Main < 0 ? 0 : 42;
                    if (row.Main >= 0)
                    {
                        VectorIcon.Draw(g, row.Main, new RectangleF(body.X + 8, y + 2, 17, 17));
                        if (row.Sub >= 0)
                            VectorIcon.Draw(g, row.Sub, new RectangleF(body.X + 28, y + 2, 17, 17));
                    }
                    int suffix = double.IsNaN(row.Multiplier) ? 0 : 60;
                    TextRenderer.DrawText(g, row.Text, Font, Rectangle.Round(new RectangleF(body.X + 8 + inset, y + (row.Main < 0 ? 0 : 3), body.Width - 16 - inset - suffix, row.Height)), Theme.TextColor, TextFlags);
                    if (suffix > 0)
                    {
                        var badge = new RectangleF(body.Right - 62, y + 1, 52, 20);
                        Drawing.Box(g, badge, Theme.MultiplierFill(row.Multiplier), Theme.MultiplierBorder(row.Multiplier), 5);
                        using (var font = UiFonts.Create("Segoe UI", 8.5f, FontStyle.Bold))
                            Drawing.Text(g, Model.Factor(row.Multiplier), font, Theme.MultiplierInk(row.Multiplier), badge, true);
                    }
                    y += row.Height;
                }
            }
            if (illustration != null)
            {
                var qrBounds = new Rectangle((Width - QrSize) / 2, (int)body.Bottom - QrSize - 10, QrSize, QrSize);
                SupportQr.Draw(g, illustration, qrBounds);
            }
        }
    }
}
