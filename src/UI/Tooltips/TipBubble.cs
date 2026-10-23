using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TipBubble : Form
    {
        private sealed class BubbleButton : SettingsButton
        {
            internal BubbleButton()
            {
                TabStop = false;
            }
            protected override void WndProc(ref Message message)
            {
                if (message.Msg != 0x201)
                {
                    base.WndProc(ref message);
                    return;
                }
                // Suppress focus only during native mouse-down. Keep normal
                // accessibility/default-action activation available otherwise.
                SetStyle(ControlStyles.Selectable, false);
                try
                {
                    base.WndProc(ref message);
                }
                finally { SetStyle(ControlStyles.Selectable, true); }
            }
        }
        internal bool ScrollGraphic;
        private readonly SlimScrollPanel graphicScroll = new SlimScrollPanel { Visible = false };
        private readonly DrawingPanel graphicContent = new DrawingPanel();
        internal float Remaining;
        private string label; private bool below; private float arrowX;
        private Action previewAction;
        private readonly SettingsButton previewButton = new BubbleButton { Text = "Radar preview", AccessibleName = "Preview suggestion on radars", Visible = false };
        private int FooterHeight
        {
            get
            {
                return applyAction != null || previewAction != null ? 44 : 0;
            }
        }
        internal void SetPreviewAction(Action action)
        {
            bool changed = (previewAction == null) != (action == null);
            previewAction = action;
            previewButton.Visible = action != null;
            if (changed && IsHandleCreated)
                RecreateHandle();
        }
        private Action applyAction;
        private readonly SettingsButton applyButton = new BubbleButton { Text = "Apply suggestion", Primary = true, AccessibleName = "Apply suggestion", Visible = false };
        internal void SetApplyAction(Action action)
        {
            bool changed = (applyAction == null) != (action == null);
            applyAction = action;
            applyButton.Visible = action != null;
            if (changed && IsHandleCreated)
                RecreateHandle();
        }
        private Action dismissAction;
        private readonly SettingsButton closeButton = new BubbleButton { Text = "×", Quiet = true, AccessibleName = "Close bubble", Visible = false, SurroundColor = () => Theme.Surface };
        internal void SetDismissAction(Action action)
        {
            bool changed = (dismissAction == null) != (action == null);
            dismissAction = action;
            closeButton.Visible = action != null;
            if (changed && IsHandleCreated)
                RecreateHandle();
        }
        private Action openAction;
        private readonly SettingsButton openButton = new BubbleButton { Glyph = "open", Text = "", Quiet = true, AccessibleName = "Open all suggestions", Visible = false, SurroundColor = () => Theme.Surface };
        internal void SetOpenAction(Action action)
        {
            bool changed = (openAction == null) != (action == null);
            openAction = action;
            openButton.Visible = action != null;
            if (changed && IsHandleCreated)
                RecreateHandle();
        }
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
            Controls.Add(graphicScroll);
            graphicScroll.Controls.Add(graphicContent);
            graphicContent.Paint += (s, e) =>
            {
                if (graphicPainter != null)
                    graphicPainter(e.Graphics, graphicContent.ClientRectangle);
            };
            Controls.Add(previewButton);
            previewButton.Click += (s, e) => { if (previewAction != null) previewAction(); };
            Controls.Add(applyButton);
            applyButton.Click += (s, e) => { if (applyAction != null) applyAction(); };
            Controls.Add(openButton);
            Controls.Add(closeButton);
            closeButton.Click += (s, e) => { if (dismissAction != null) dismissAction(); };
            openButton.Click += (s, e) => { if (openAction != null) openAction(); };
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 8.25f, FontStyle.Bold));
        }
        protected override void WndProc(ref Message message)
        {
            // A native child Button asks its popup to activate before mouse-down.
            // Keep the owner active, but deliver the click (MA_NOACTIVATE, not EAT).
            if (message.Msg == 0x21)
            {
                message.Result = new IntPtr(3);
                return;
            }
            base.WndProc(ref message);
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
                p.ExStyle |= 0x08000000 | 0x80;
                if (openAction == null && dismissAction == null && applyAction == null && previewAction == null)
                    p.ExStyle |= 0x20;
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
            textWidth = maxTextWidth;
            Size = new Size(textWidth + 20, ShortcutText.Measure(text, Font, textWidth) + 22);
            if (rows.Count > 0)
            {
                int width = 0, height = 0;
                foreach (var row in rows)
                {
                    int inset = row.Main < 0 ? 0 : 42;
                    int suffix = double.IsNaN(row.Multiplier) ? 0 : 60;
                    var size = TextRenderer.MeasureText(row.Text.Length == 0 ? " " : row.Text, Font, new Size(maxTextWidth - inset - suffix, int.MaxValue), TextFlags);
                    row.Height = Math.Max(row.Main < 0 ? 0 : 24, ShortcutText.Measure(row.Text, Font, maxTextWidth - inset - suffix) + 3);
                    width = Math.Max(width, size.Width + inset + suffix);
                    height += row.Height;
                }
                Size = new Size(maxTextWidth + 24, height + 24);
            }
            if (illustration != null)
                Size = new Size(Math.Max(QrSize + 24, Width), Height + QrSize + 12);
            if (graphicPainter != null && graphicSize.HasValue)
                Size = new Size(graphicSize.Value.Width + 24, graphicSize.Value.Height + 24);
            graphicScroll.Visible = ScrollGraphic && graphicPainter != null && graphicSize.HasValue;
            if (ScrollGraphic && graphicPainter != null && graphicSize.HasValue)
            {
                Width += 16;
                Height = Math.Min(Height + 30, Math.Min(540, screen.Height - 28));
                graphicScroll.SetBounds(10, 38, Width - 18, Height - 54);
                graphicContent.Size = graphicSize.Value;
                graphicScroll.AutoScrollPosition = Point.Empty;
                graphicContent.Invalidate();
            }
            Height += FooterHeight;
            int actionWidth = applyAction != null && previewAction != null ? (Width - 32) / 2 : Width - 24;
            previewButton.SetBounds(12, Height - 48, actionWidth, 32);
            applyButton.SetBounds(previewAction != null ? 20 + actionWidth : 12, Height - 48, actionWidth, 32);
            Width += 6; // Reserve the side-arrow gutter without reducing the measured text area.
            closeButton.SetBounds(Width - 39, 9, 26, 26);
            openButton.SetBounds(Width - (dismissAction == null ? 39 : 67), 9, 26, 26);
            below = bounds.Bottom + 7 + Height <= screen.Bottom - 7;
            int left = Math.Max(screen.Left + 7, Math.Min(bounds.Left + (bounds.Width - Width) / 2, screen.Right - Width - 7));
            int top = below ? bounds.Bottom + 7 : Math.Max(screen.Top + 7, bounds.Top - Height - 7);
            arrowX = Math.Max(12, Math.Min(Width - 14, bounds.Left + bounds.Width / 2f - left));
            Location = new Point(left, top);
            Relocate(Bounds);
            Show(ApplicationTips.OwnerForm(anchor));
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
            if (Remaining > 0)
                Drawing.ProgressOutline(g, new RectangleF(body.X + 1, body.Y + 1, body.Width - 2, body.Height - 2), 6, Remaining, Color.FromArgb(145, Theme.Accent), 1.5f);
            var points = ArrowPoints(body);
            using (var fill = new SolidBrush(Theme.Surface))
                g.FillPolygon(fill, points);
            using (var erase = new Pen(Theme.Surface, 2))
                g.DrawLine(erase, points[0], points[2]);
            using (var border = new Pen(Theme.StrongBorder))
                g.DrawLines(border, points);
            if (graphicPainter != null)
            {
                if (graphicScroll.Visible)
                    return;
                graphicPainter(g, new RectangleF(body.X + 8, body.Y + 6, body.Width - 16, body.Height - 12 - FooterHeight));
                return;
            }
            if (rows.Count == 0)
                ShortcutText.Layout(g, label, Font, Theme.TextColor, new RectangleF(body.X + 8, body.Y + 6, body.Width - 16, body.Height - 12), true);
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
                    ShortcutText.Layout(g, row.Text, Font, Theme.TextColor, new RectangleF(body.X + 8 + inset, y + (row.Main < 0 ? 0 : 3), body.Width - 16 - inset - suffix, row.Height), true);
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
