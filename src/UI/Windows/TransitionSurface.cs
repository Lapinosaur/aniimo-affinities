using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace AniimoAffinities
{
    // Present one complete layout at a time. Never morph individual text/card rectangles.
    internal sealed class TransitionSurface : DrawingPanel
    {
        internal Bitmap Before, After;
        internal double Progress;
        internal TransitionSurface()
        {
            PhysicalPainting = true;
            TabStop = false;
            Dock = DockStyle.None;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.Background);
            bool entering = Progress >= .5;
            var image = entering ? After : Before;
            if (image == null)
                return;
            double phase = entering ? (Progress - .5) * 2 : Progress * 2;
            float alpha = (float)(entering ? phase : 1 - phase);
            float scale = Math.Min(ClientSize.Width / (float)image.Width, ClientSize.Height / (float)image.Height);
            int width = (int)Math.Round(image.Width * scale), height = (int)Math.Round(image.Height * scale);
            int offset = (int)Math.Round((entering ? 1 - phase : -phase) * 8);
            var target = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2 + offset, width, height);
            using (var attributes = new ImageAttributes())
            {
                var matrix = new ColorMatrix { Matrix33 = alpha };
                attributes.SetColorMatrix(matrix);
                e.Graphics.DrawImage(image, target, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }
        internal void ReleaseImages()
        {
            if (Before != null)
                Before.Dispose();
            if (After != null)
                After.Dispose();
            Before = After = null;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ReleaseImages();
            base.Dispose(disposing);
        }
    }
}
