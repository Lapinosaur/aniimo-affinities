using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TypeSelector : DrawingPanel
    {
        internal bool IconModes;
        private RectangleF groupBounds;
        internal void Arrange(bool selected)
        {
            float zoom = WindowZoom.Factor(this);
            int x = 13;
            var buttons = Controls.OfType<IconButton>().Where(button => IconModes || button.Type >= 0).OrderBy(button => button.TabIndex).ToArray();
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                button.Segment = true;
                button.SegmentStart = i == 0;
                button.SegmentEnd = i == buttons.Length - 1 && !selected;
                button.SetBounds((int)(x * zoom), (int)(11 * zoom), (int)(42 * zoom), (int)(36 * zoom));
                x += 42;
            }
            var reset = IconModes ? null : Controls.OfType<IconButton>().FirstOrDefault(button => button.Type < 0);
            if (reset != null)
            {
                reset.Visible = selected;
                reset.Segment = true;
                reset.SegmentStart = false;
                reset.SegmentEnd = true;
                reset.SetBounds((int)(x * zoom), (int)(11 * zoom), (int)(40 * zoom), (int)(36 * zoom));
                if (selected)
                    x += 40;
            }
            groupBounds = new RectangleF(12, 10, x - 11, 38);
            foreach (var button in Controls.OfType<IconButton>())
            {
                button.GroupShape = new RectangleF(groupBounds.X - button.Left / zoom, groupBounds.Y - button.Top / zoom, groupBounds.Width, groupBounds.Height);
                button.Invalidate();
            }
            if (base.Width != (int)((x + 12) * zoom))
                base.Width = (int)((x + 12) * zoom);
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float zoom = WindowZoom.Factor(this);
            e.Graphics.ScaleTransform(zoom, zoom);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (groupBounds.Width > 0)
                Drawing.Box(e.Graphics, groupBounds, Theme.Surface, Theme.Border, Theme.SelectorRadius);
        }
    }
}
