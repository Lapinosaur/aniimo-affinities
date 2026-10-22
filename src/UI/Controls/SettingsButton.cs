using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SettingsButton : Button
    {
        internal bool Primary, Quiet, Segment;
        internal RectangleF GroupShape;
        internal static void Group(params SettingsButton[] buttons)
        {
            int left = buttons[0].Left, right = buttons[buttons.Length - 1].Right, top = buttons[0].Top, height = buttons[0].Height;
            for (int i = 0; i < buttons.Length; i++)
            {
                int x = left + (right - left) * i / buttons.Length, end = left + (right - left) * (i + 1) / buttons.Length;
                buttons[i].SetBounds(x, top, end - x, height);
                buttons[i].Segment = true;
                buttons[i].GroupShape = new RectangleF(left - x, 0, right - left - 1, height - 1);
            }
        }
        internal string Glyph = "";
        private bool hover, pressed;
        internal SettingsButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = false;
            pressed = false;
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            pressed = e.Button == MouseButtons.Left;
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
            Invalidate();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.No;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            float zoom = WindowZoom.Factor(this);
            int Width = (int)Math.Round(base.Width / zoom), Height = (int)Math.Round(base.Height / zoom);
            var g = e.Graphics;
            g.ScaleTransform(zoom, zoom);
            g.Clear(Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Primary ? (pressed ? Theme.AccentPressed : hover ? Theme.AccentLight : Theme.Accent) : hover ? Theme.HoverSurface : Quiet ? Theme.Background : Theme.Surface;
            Color border = Focused && ShowFocusCues ? Theme.Accent : Primary ? fill : Quiet ? fill : Theme.Border;
            if (!Enabled)
            {
                fill = Theme.Surface;
                border = Theme.Border;
            }
            if (Segment)
            {
                Drawing.Box(g, GroupShape, Theme.Surface, Theme.Border, Theme.ControlRadius);
                var inner = GroupShape;
                inner.Inflate(-1, -1);
                using (var clip = Drawing.Round(inner, Theme.ControlRadius - 1))
                    g.SetClip(clip);
                using (var brush = new SolidBrush(Primary ? Theme.AccentSurface : hover ? Theme.HoverSurface : Theme.Surface))
                    g.FillRectangle(brush, 0, 1, Width, Height - 2);
                if (Primary)
                    using (var pen = new Pen(Theme.Accent, 2))
                        g.DrawLine(pen, 0, Height - 2, Width, Height - 2);
                using (var pen = new Pen(Theme.Border))
                    g.DrawLine(pen, Width - 1, 7, Width - 1, Height - 8);
            }
            else
                Drawing.Box(g, new RectangleF(2, 2, Width - 5, Height - 5), fill, border, 6);
            if (Focused && ShowFocusCues)
                using (var outline = Drawing.Round(new RectangleF(0, 0, Width - 1, Height - 1), 8))
                using (var pen = new Pen(Theme.AccentForeground))
                    g.DrawPath(pen, outline);
            if (Glyph.Length > 0)
                Glyphs.Draw(g, Glyph, new RectangleF((Width - 20) / 2f, (Height - 20) / 2f, 20, 20), Theme.TextColor);
            else if (Quiet)
                Glyphs.Draw(g, "reset", new RectangleF((Width - 16) / 2f, (Height - 16) / 2f, 16, 16), hover ? Theme.TextColor : Theme.Muted);
            else
                Drawing.Text(g, Text, Font, !Enabled ? Theme.Muted : Primary ? (Segment ? Theme.AccentForeground : Theme.AccentInk) : Theme.TextColor, new RectangleF(4, 0, Width - 8, Height), true);
        }
    }
}
