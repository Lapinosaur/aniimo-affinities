using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal class SettingsButton : InteractiveButton
    {
        internal bool Primary, Quiet, Segment;
        protected override bool IsBusy
        {
            get
            {
                return Busy;
            }
        }
        protected override bool IsSelected
        {
            get
            {
                return Segment && Primary;
            }
        }
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
        internal bool GlyphWithText;
        internal Color Brand = Color.Empty;
        internal Color Surround = Color.Empty;
        private bool hover, pressed;
        private readonly Timer busyTimer = new Timer { Interval = 40 };
        private string idleText, idleDescription;
        private bool idleEnabled;
        private int busyAngle;
        internal bool Busy
        {
            get; private set;
        }
        internal bool BeginBusy(string text)
        {
            if (Busy || IsDisposed || !Enabled)
                return false;
            idleText = Text;
            idleDescription = AccessibleDescription;
            idleEnabled = Enabled;
            Busy = true;
            Text = text;
            AccessibleDescription = text;
            Enabled = false;
            pressed = hover = false;
            if (Visible)
                busyTimer.Start();
            Invalidate();
            Update();
            return true;
        }
        internal void EndBusy()
        {
            if (!Busy || IsDisposed)
                return;
            busyTimer.Stop();
            Busy = false;
            Text = idleText;
            AccessibleDescription = idleDescription;
            Enabled = idleEnabled;
            Invalidate();
        }
        internal SettingsButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            busyTimer.Tick += (s, e) => { busyAngle = (busyAngle + 16) % 360; Invalidate(); };
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Busy && Visible)
                busyTimer.Start();
            else
                busyTimer.Stop();
        }
        protected override void OnClick(EventArgs e)
        {
            if (!Busy && Enabled)
                base.OnClick(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                busyTimer.Dispose();
            base.Dispose(disposing);
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
            Cursor = Busy ? Cursors.AppStarting : Enabled ? Cursors.Hand : Cursors.No;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            float zoom = WindowZoom.Factor(this);
            int Width = (int)Math.Round(base.Width / zoom), Height = (int)Math.Round(base.Height / zoom);
            var g = e.Graphics;
            g.ScaleTransform(zoom, zoom);
            g.Clear(Surround.IsEmpty ? Theme.Background : Surround);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Primary ? ((pressed || InteractionPressed) ? Theme.AccentPressed : hover ? Theme.AccentLight : Theme.Accent) : hover ? Theme.HoverSurface : Quiet ? (Surround.IsEmpty ? Theme.Background : Surround) : Theme.Surface;
            if (!Brand.IsEmpty)
                fill = Theme.BrandSurface(Brand, hover || pressed);
            Color border = Focused && ShowFocusCues ? Theme.Accent : Primary ? fill : Quiet ? fill : Theme.Border;
            if (!Brand.IsEmpty)
                border = Theme.BrandInk(Brand, fill);
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
                using (var brush = new SolidBrush(!Enabled ? Theme.Surface : Primary ? Theme.AccentSurface : hover ? Theme.HoverSurface : Theme.Surface))
                    g.FillRectangle(brush, 0, 1, Width, Height - 2);
                if (Primary && Enabled)
                    using (var pen = new Pen(Theme.Accent, 2))
                        g.DrawLine(pen, 0, Height - 2, Width, Height - 2);
                using (var pen = new Pen(Theme.Border))
                    g.DrawLine(pen, Width - 1, 7, Width - 1, Height - 8);
            }
            else
                Drawing.Box(g, new RectangleF(2, 2, Width - 5, Height - 5), fill, border, 6);
            if (Busy)
            {
                bool hasText = Text.Length > 0;
                float x = hasText ? 12 : (Width - 16) / 2f;
                using (var pen = new Pen(Theme.AccentForeground, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(pen, x, (Height - 16) / 2f, 16, 16, busyAngle, 265);
                if (hasText)
                    Drawing.Text(g, Text, Font, Theme.TextColor, new RectangleF(34, 0, Math.Max(1, Width - 40), Height), true);
            }
            else if (Glyph.Length > 0 && GlyphWithText)
            {
                Color ink = !Enabled ? Theme.Muted : Primary ? Theme.AccentInk : Theme.TextColor;
                Glyphs.Draw(g, Glyph, new RectangleF(14, (Height - 18) / 2f, 18, 18), Brand.IsEmpty || !Enabled ? ink : Theme.BrandInk(Brand, fill));
                Drawing.Text(g, Text, Font, ink, new RectangleF(40, 0, Width - 46, Height), true);
            }
            else if (Glyph.Length > 0)
                Glyphs.Draw(g, Glyph, new RectangleF((Width - 20) / 2f, (Height - 20) / 2f, 20, 20), !Enabled ? Theme.Muted : Primary ? (Segment ? Theme.AccentForeground : Theme.AccentInk) : Theme.TextColor);
            else if (Quiet)
                Glyphs.Draw(g, "reset", new RectangleF((Width - 16) / 2f, (Height - 16) / 2f, 16, 16), hover ? Theme.TextColor : Theme.Muted);
            else
                Drawing.Text(g, Text, Font, !Enabled ? Theme.Muted : Primary ? (Segment ? Theme.AccentForeground : Theme.AccentInk) : Theme.TextColor, new RectangleF(4, 0, Width - 8, Height), true);
            FinishPaint(g, Width, Height, false);
        }
    }
}
