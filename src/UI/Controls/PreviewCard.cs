using System;
using System.Drawing;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class PreviewCard
    {
        internal static void Attach(Control control)
        {
            EventHandler refresh = (s, e) => control.Invalidate();
            control.MouseEnter += refresh;
            control.MouseLeave += refresh;
            control.GotFocus += refresh;
            control.LostFocus += refresh;
            control.EnabledChanged += (s, e) => { control.Cursor = control.Enabled ? Cursors.Hand : Cursors.Default; control.Invalidate(); };
        }
        internal static void Draw(Graphics g, RectangleF bounds, bool selected, bool enabled, bool hovered, bool focus)
        {
            var card = bounds;
            card.Inflate(-2, -2);
            Drawing.Box(g, card, selected && enabled ? Theme.AccentSurface : Theme.Surface, enabled && (selected || hovered) ? Theme.Accent : Theme.Border, Theme.ControlRadius);
            if (focus && enabled)
                using (var outline = Drawing.Round(bounds, Theme.ControlRadius + 2))
                using (var pen = new Pen(Theme.AccentForeground))
                    g.DrawPath(pen, outline);
        }
        internal static Color LabelColor(bool selected, bool enabled)
        {
            return selected && enabled ? Theme.AccentForeground : Theme.Muted;
        }
    }
}
