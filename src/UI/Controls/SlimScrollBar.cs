using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SlimScrollBar : Control
    {
        private int value, maximum, viewport = 1, dragOffset, wheelDelta;
        private bool hover, dragging;
        internal event Action<int> ValueChanged;
        internal SlimScrollBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;
            AccessibleRole = AccessibleRole.ScrollBar;
            AccessibleName = "Scroll rows";
            Cursor = Cursors.Hand;
        }
        internal void Configure(int current, int limit, int page)
        {
            maximum = Math.Max(0, limit);
            viewport = Math.Max(1, page);
            value = Math.Max(0, Math.Min(maximum, current));
            Visible = maximum > 0;
            AccessibleDescription = "Row " + (value + 1) + ". Use the mouse wheel or the table's arrow and Page keys.";
            Invalidate();
        }
        private RectangleF Thumb
        {
            get
            {
                float track = Math.Max(1, Height - 4);
                float length = Math.Min(track, Math.Max(24, track * viewport / (maximum + viewport)));
                return new RectangleF((Width - (hover || dragging ? 6 : 4)) / 2f, 2 + (maximum == 0 ? 0 : (track - length) * value / maximum), hover || dragging ? 6 : 4, length);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            DrawScroll(e.Graphics);
        }
        internal void DrawScroll(Graphics graphics)
        {
            using (var background = new SolidBrush(BackColor))
                graphics.FillRectangle(background, ClientRectangle);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (maximum <= 0)
                return;
            var thumb = Thumb;
            using (var path = Drawing.Round(thumb, thumb.Width / 2))
            using (var fill = new SolidBrush(dragging || hover || SystemInformation.HighContrast ? Theme.Muted : Theme.StrongBorder))
                graphics.FillPath(fill, path);
        }
        private void Change(int next)
        {
            next = Math.Max(0, Math.Min(maximum, next));
            if (next == value)
                return;
            value = next;
            Invalidate();
            if (ValueChanged != null)
                ValueChanged(value);
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
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left)
                return;
            if (e.Y >= Thumb.Top && e.Y <= Thumb.Bottom)
            {
                dragging = true;
                dragOffset = e.Y - (int)Thumb.Y;
                Capture = true;
            }
            else
                Change(value + (e.Y < Thumb.Y ? -viewport : viewport));
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
            {
                float travel = Height - 4 - Thumb.Height;
                if (travel > 0)
                    Change((int)Math.Round((e.Y - dragOffset - 2) * maximum / travel));
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
                Capture = false;
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            dragging = false;
            Invalidate();
            base.OnMouseCaptureChanged(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int lines = SystemInformation.MouseWheelScrollLines;
            wheelDelta += e.Delta;
            int steps = wheelDelta / 120;
            wheelDelta %= 120;
            Change(value - steps * (lines < 0 ? viewport : lines));
            var handled = e as HandledMouseEventArgs;
            if (handled != null)
                handled.Handled = true;
        }
    }
}
