using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AniimoAffinities
{
    internal static class GlassFrame
    {
        private const int SurfaceAlpha = 96, InsetAlpha = 115, HoverAlpha = 130;
        private static Color GlassInk(Color ink)
        {
            if (ink != Theme.Muted)
                return ink;
            return Color.FromArgb((ink.R * 65 + Theme.TextColor.R * 35) / 100,
                (ink.G * 65 + Theme.TextColor.G * 35) / 100,
                (ink.B * 65 + Theme.TextColor.B * 35) / 100);
        }
        internal static Rectangle FitBlurBounds(Region silhouette, Rectangle bounds, int radius)
        {
            // Native material has its own corner geometry. Keep its entire
            // curved edge inside the actual painted silhouette, including Win10
            // where the native corner preference is unavailable.
            for (int inset = 0; inset < 16 && bounds.Width > 4 && bounds.Height > 4; inset++)
            {
                bool fits = true;
                int corner = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
                for (int x = 0; x <= corner && fits; x++)
                    for (int y = 0; y <= corner && fits; y++)
                    {
                        if (corner > 0 && Math.Pow(corner - x - .5, 2) + Math.Pow(corner - y - .5, 2) > corner * corner)
                            continue;
                        fits = silhouette.IsVisible(bounds.Left + x + .5f, bounds.Top + y + .5f) &&
                            silhouette.IsVisible(bounds.Right - x - .5f, bounds.Top + y + .5f) &&
                            silhouette.IsVisible(bounds.Left + x + .5f, bounds.Bottom - y - .5f) &&
                            silhouette.IsVisible(bounds.Right - x - .5f, bounds.Bottom - y - .5f);
                    }
                if (fits)
                    return bounds;
                bounds.Inflate(-1, -1);
            }
            return bounds;
        }
        internal static List<Rectangle> Surfaces(Region region)
        {
            var result = new List<Rectangle>();
            using (var matrix = new Matrix())
                foreach (var scan in region.GetRegionScans(matrix))
                {
                    var current = Rectangle.Ceiling(scan);
                    for (int i = 0; i < result.Count; i++)
                    {
                        var contact = result[i];
                        contact.Inflate(1, 1);
                        if (!contact.IntersectsWith(current))
                            continue;
                        current = Rectangle.Union(current, result[i]);
                        result.RemoveAt(i);
                        i = -1;
                    }
                    result.Add(current);
                }
            result.RemoveAll(rect => rect.Width < 20 || rect.Height < 20);
            return result;
        }
        // Convert only surface fills to premultiplied translucency. Foreground
        // text, glyphs, type colors and borders retain their original opacity.
        internal static Region Prepare(Bitmap image, bool wholeWindow = false)
        {
            var backgrounds = new[] { Theme.Surface, Theme.InsetSurface, Theme.Background };
            var alphas = new[] { SurfaceAlpha, InsetAlpha, wholeWindow ? SurfaceAlpha : 0 };
            var inks = new[] { Theme.Border, Theme.Muted, Theme.TextColor, Theme.ReducedDamage, Theme.IncreasedDamage, Theme.AccentForeground, Theme.StrongBorder, Theme.Surface, Theme.InsetSurface, Theme.HoverSurface };
            var rectangles = new List<Rectangle>();
            var previousRuns = new Dictionary<long, int>();
            var runs = new Dictionary<long, int>();
            var colors = new Dictionary<int, int>();
            var bounds = new Rectangle(Point.Empty, image.Size);
            var data = image.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                var row = new int[image.Width];
                int key = Theme.Background.ToArgb() & 0xffffff;
                int surface = Theme.Surface.ToArgb() & 0xffffff, inset = Theme.InsetSurface.ToArgb() & 0xffffff;
                int hover = Theme.HoverSurface.ToArgb() & 0xffffff;
                for (int y = 0; y < image.Height; y++)
                {
                    runs.Clear();
                    IntPtr address = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(address, row, 0, row.Length);
                    int start = -1;
                    for (int x = 0; x <= row.Length; x++)
                    {
                        bool visible = x < row.Length && (wholeWindow || (row[x] & 0xffffff) != key);
                        if (visible && start < 0)
                            start = x;
                        if (!visible && start >= 0)
                        {
                            long run = ((long)start << 32) | (uint)(x - start);
                            int index;
                            if (previousRuns.TryGetValue(run, out index))
                            {
                                var rectangle = rectangles[index];
                                rectangle.Height++;
                                rectangles[index] = rectangle;
                            }
                            else
                            {
                                index = rectangles.Count;
                                rectangles.Add(new Rectangle(start, y, x - start, 1));
                            }
                            runs.Add(run, index);
                            start = -1;
                        }
                        if (x == row.Length)
                            break;
                        int pixel = row[x] & 0xffffff;
                        int alpha = !visible ? 0 : pixel == key ? SurfaceAlpha : pixel == surface ? SurfaceAlpha : pixel == inset ? InsetAlpha : pixel == hover ? HoverAlpha : 255;
                        if (alpha == 255)
                        {
                            int converted;
                            if (!colors.TryGetValue(pixel, out converted))
                            {
                                TryUnmatte(pixel, backgrounds, alphas, inks, out converted);
                                colors.Add(pixel, converted);
                            }
                            row[x] = converted;
                            continue;
                        }
                        row[x] = (alpha << 24) | (((pixel >> 16) & 255) * alpha / 255 << 16) |
                            (((pixel >> 8) & 255) * alpha / 255 << 8) | ((pixel & 255) * alpha / 255);
                    }
                    Marshal.Copy(row, 0, address, row.Length);
                    var reusable = previousRuns;
                    previousRuns = runs;
                    runs = reusable;
                }
            }
            finally { image.UnlockBits(data); }
            using (var path = new GraphicsPath(FillMode.Winding))
            {
                if (rectangles.Count > 0)
                    path.AddRectangles(rectangles.ToArray());
                return new Region(path);
            }
        }
        // GDI antialiasing was rendered against the opaque theme. Remove that
        // original matte at edges before compositing onto the glass surface.
        private static bool TryUnmatte(int pixel, Color[] backgrounds, int[] alphas, Color[] inks, out int result)
        {
            result = pixel | unchecked((int)0xff000000);
            foreach (var ink in inks)
                if ((ink.ToArgb() & 0xffffff) == pixel)
                {
                    result = GlassInk(ink).ToArgb();
                    return false;
                }
            int r = (pixel >> 16) & 255, g = (pixel >> 8) & 255, b = pixel & 255;
            double best = double.MaxValue;
            bool matched = false;
            for (int i = 0; i < backgrounds.Length; i++)
            {
                Color bg = backgrounds[i];
                foreach (var ink in inks)
                {
                    double dr = ink.R - bg.R, dg = ink.G - bg.G, db = ink.B - bg.B;
                    double length = dr * dr + dg * dg + db * db;
                    if (length < 1)
                        continue;
                    double t = ((r - bg.R) * dr + (g - bg.G) * dg + (b - bg.B) * db) / length;
                    if (t <= 0 || t >= 1)
                        continue;
                    double er = r - bg.R - t * dr, eg = g - bg.G - t * dg, eb = b - bg.B - t * db;
                    double error = er * er + eg * eg + eb * eb;
                    double score = error + (1 - t) * .01;
                    if (error > 1.5 || score >= best)
                        continue;
                    double removed = (1 - t) * (1 - alphas[i] / 255.0);
                    int inkAlpha = ink == Theme.Surface ? SurfaceAlpha : ink == Theme.InsetSurface ? InsetAlpha : ink == Theme.HoverSurface ? HoverAlpha : 255;
                    double removedInk = t * (1 - inkAlpha / 255.0);
                    int a = (int)Math.Round(255 * (1 - removed - removedInk));
                    Color outputInk = GlassInk(ink);
                    int rr = Math.Max(0, Math.Min(a, (int)Math.Round(r - removed * bg.R - removedInk * ink.R + t * (outputInk.R - ink.R))));
                    int gg = Math.Max(0, Math.Min(a, (int)Math.Round(g - removed * bg.G - removedInk * ink.G + t * (outputInk.G - ink.G))));
                    int bb = Math.Max(0, Math.Min(a, (int)Math.Round(b - removed * bg.B - removedInk * ink.B + t * (outputInk.B - ink.B))));
                    result = (a << 24) | (rr << 16) | (gg << 8) | bb;
                    best = score;
                    matched = true;
                }
            }
            return matched;
        }
    }
}
