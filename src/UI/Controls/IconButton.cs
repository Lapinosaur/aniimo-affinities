using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class IconButton : Button
    {
        internal int Type = -1; internal string Badge = "", Glyph = ""; internal bool Active, Segment, SegmentStart, SegmentEnd; private bool hover;
        internal RectangleF GroupShape;
        internal string Shortcut = "";
        internal IconButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            TabStop = true;
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            float zoom = WindowZoom.Factor(this);
            int Width = (int)Math.Round(base.Width / zoom), Height = (int)Math.Round(base.Height / zoom);
            var g = e.Graphics;
            g.ScaleTransform(zoom, zoom);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Segment && !SegmentStart && !SegmentEnd ? Theme.Surface : Theme.Background);
            if (Segment)
            {
                if (SegmentStart || SegmentEnd)
                {
                    // Paint the parent's exact contour in local coordinates; do not invent a second corner.
                    Drawing.Box(g, GroupShape, Theme.Surface, Theme.Border, Theme.SelectorRadius);
                    var inner = GroupShape;
                    inner.Inflate(-1, -1);
                    using (var clip = Drawing.Round(inner, Theme.SelectorRadius - 1))
                        g.SetClip(clip);
                }
                using (var fill = new SolidBrush(Active ? Theme.AccentSurface : hover ? Theme.HoverSurface : Theme.Surface))
                    g.FillRectangle(fill, 0, 0, Width, Height);
                if (!SegmentEnd)
                    using (var divider = new Pen(Theme.Surface))
                        g.DrawLine(divider, Width - 1, 7, Width - 1, Height - 7);
                if (Active)
                    using (var underline = new Pen(Theme.Accent, 2))
                        g.DrawLine(underline, 0, Height - 1, Width, Height - 1);
                if (Focused && ShowFocusCues)
                    using (var outline = Drawing.Round(new RectangleF(3, 3, Width - 7, Height - 7), 5))
                    using (var focus = new Pen(Theme.AccentForeground, 1.5f))
                        g.DrawPath(focus, outline);
            }
            else
                Drawing.Box(g, new RectangleF(2, 3, Width - 5, Height - 7), Active ? Theme.AccentSurface : hover ? Theme.HoverSurface : Theme.Surface, Focused ? Theme.AccentForeground : Active ? Theme.Accent : Theme.Border, 8);
            if (Type >= 0)
                VectorIcon.Draw(g, Type, new RectangleF(11, (Height - 20) / 2f, 20, 20));
            else
                Glyphs.Draw(g, Glyph, new RectangleF(11, (Height - 20) / 2f, 20, 20), Active ? Theme.Accent : Theme.Muted);
            if (Badge.Length > 0)
            {
                var badgeBounds = new RectangleF(Width - 27, Height - 13, 25, 11);
                Drawing.Box(g, badgeBounds, Theme.AccentSurface, Theme.AccentSurface, 4);
                using (var badgeFont = UiFonts.Create(Font.FontFamily, 6f, FontStyle.Bold))
                    Drawing.Text(g, Badge, badgeFont, Theme.AccentForeground, badgeBounds, true);
            }
            if (DisplaySettings.ShowBadges && Shortcut.Length > 0 && Form.ActiveForm == FindForm())
            {
                g.ResetClip();
                int badgeWidth = Math.Max(11, Shortcut.Length * 6 + 3);
                Drawing.Box(g, new RectangleF(Width - badgeWidth - 2, 1, badgeWidth, 11), Theme.Border, Theme.Border, 3);
                using (var keyFont = UiFonts.Create("Segoe UI", 6.5f, FontStyle.Bold))
                    Drawing.Text(g, Shortcut, keyFont, Theme.TextColor, new RectangleF(Width - badgeWidth - 2, 1, badgeWidth, 11), true);
            }
        }
    }
}
