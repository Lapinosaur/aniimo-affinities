using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class RoundCheckBox : CheckBox
    {
        internal Func<Color> SurfaceColor;
        internal RoundCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.CheckButton;
        }
        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
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
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        protected override void OnChangeUICues(UICuesEventArgs e)
        {
            base.OnChangeUICues(e);
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Cursor = Enabled && AutoCheck ? Cursors.Hand : Cursors.Default;
            var g = e.Graphics;
            Color surface = SurfaceColor == null ? BackColor : SurfaceColor();
            g.Clear(surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float zoom = WindowZoom.Factor(this);
            float diameter = Math.Max(14 * zoom, Math.Min(20 * zoom, Font.Height));
            var circle = new RectangleF(3 * zoom, (Height - diameter) / 2, diameter, diameter);
            using (var brush = new SolidBrush(Checked && Enabled ? Theme.Accent : SurfaceColor == null ? Theme.Surface : surface))
                g.FillEllipse(brush, circle);
            using (var pen = new Pen(Enabled ? Checked ? Theme.AccentForeground : Theme.StrongBorder : Theme.Border, 1.3f * zoom))
                g.DrawEllipse(pen, circle);
            if (Checked)
                using (var pen = new Pen(Enabled ? Theme.AccentInk : Theme.Muted, 1.8f * zoom) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(pen, new[] { new PointF(circle.X + diameter * .25f, circle.Y + diameter * .5f), new PointF(circle.X + diameter * .43f, circle.Y + diameter * .68f), new PointF(circle.X + diameter * .76f, circle.Y + diameter * .32f) });
            if (Focused && ShowFocusCues)
            {
                circle.Inflate(2 * zoom, 2 * zoom);
                using (var pen = new Pen(Theme.AccentForeground))
                    g.DrawEllipse(pen, circle);
            }
            if (!string.IsNullOrEmpty(Text))
                TextRenderer.DrawText(g, Text, Font, new Rectangle((int)(diameter + 12 * zoom), 0, Math.Max(0, Width - (int)(diameter + 12 * zoom)), Height), Enabled ? ForeColor : Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
