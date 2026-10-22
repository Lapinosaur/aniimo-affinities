using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class RoundCheckBox : CheckBox
    {
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
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float diameter = Math.Max(14, Math.Min(20, Font.Height));
            var circle = new RectangleF(3, (Height - diameter) / 2, diameter, diameter);
            using (var brush = new SolidBrush(Checked && Enabled ? Theme.Accent : Theme.Surface))
                g.FillEllipse(brush, circle);
            using (var pen = new Pen(Enabled ? Checked ? Theme.AccentForeground : Theme.StrongBorder : Theme.Border, 1.3f))
                g.DrawEllipse(pen, circle);
            if (Checked)
                using (var pen = new Pen(Enabled ? Theme.AccentInk : Theme.Muted, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(pen, new[] { new PointF(circle.X + diameter * .25f, circle.Y + diameter * .5f), new PointF(circle.X + diameter * .43f, circle.Y + diameter * .68f), new PointF(circle.X + diameter * .76f, circle.Y + diameter * .32f) });
            if (Focused && ShowFocusCues)
            {
                circle.Inflate(2, 2);
                using (var pen = new Pen(Theme.AccentForeground))
                    g.DrawEllipse(pen, circle);
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle((int)diameter + 12, 0, Width - (int)diameter - 12, Height), Enabled ? ForeColor : Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
