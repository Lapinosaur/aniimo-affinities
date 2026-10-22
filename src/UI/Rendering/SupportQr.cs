using System.Drawing;
using System.Drawing.Drawing2D;
namespace AniimoAffinities
{
    internal static class SupportQr
    {
        internal static void Draw(Graphics g, Image image, Rectangle qrBounds)
        {
            bool light = Theme.Background.GetBrightness() > .5f;
            var paper = light ? Theme.Background : Color.FromArgb(172, 186, 195);
            var ink = Color.FromArgb(36, 51, 65);
            var state = g.Save();
            using (var clip = Drawing.Round(qrBounds, 9))
            using (var colors = new System.Drawing.Imaging.ImageAttributes())
            {
                colors.SetRemapTable(new[] {
                        new System.Drawing.Imaging.ColorMap { OldColor = Color.White, NewColor = paper },
                        new System.Drawing.Imaging.ColorMap { OldColor = Color.Black, NewColor = ink }
                    });
                g.SetClip(clip);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(image, qrBounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, colors);
            }
            g.Restore(state);
        }
    }
}
