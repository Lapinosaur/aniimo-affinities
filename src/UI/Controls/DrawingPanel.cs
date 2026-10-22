using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal class DrawingPanel : Panel
    {
        internal bool PhysicalPainting;
        private bool painting;
        public new int Width
        {
            get
            {
                return painting ? (int)Math.Round(base.Width / WindowZoom.Factor(this)) : base.Width;
            }
            set
            {
                base.Width = value;
            }
        }
        public new int Height
        {
            get
            {
                return painting ? (int)Math.Round(base.Height / WindowZoom.Factor(this)) : base.Height;
            }
            set
            {
                base.Height = value;
            }
        }
        public new Rectangle ClientRectangle
        {
            get
            {
                return new Rectangle(0, 0, Width, Height);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var state = e.Graphics.Save();
            painting = !PhysicalPainting;
            if (painting)
            {
                float zoom = WindowZoom.Factor(this);
                e.Graphics.ScaleTransform(zoom, zoom);
            }
            try
            {
                base.OnPaint(e);
            }
            finally { painting = false; e.Graphics.Restore(state); }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            float z = PhysicalPainting ? 1 : WindowZoom.Factor(this);
            base.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks, (int)(e.X / z), (int)(e.Y / z), e.Delta));
        }
        internal DrawingPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
