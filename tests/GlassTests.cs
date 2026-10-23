using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class GlassTests
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
        internal static void Run()
        {
            using (var outline = Drawing.Round(new RectangleF(0, 0, 140, 90), 16))
            using (var silhouette = new Region(outline))
                foreach (int radius in new[] { 0, 8 })
                {
                    var fitted = GlassFrame.FitBlurBounds(silhouette, new Rectangle(0, 0, 140, 90), radius);
                    if (fitted.Left <= 0 || fitted.Width <= 100 || fitted.Height <= 50)
                        throw new Exception("Glass must fit inside custom corners without losing the surface interior.");
                    for (int x = 0; x <= radius; x++)
                        for (int y = 0; y <= radius; y++)
                        {
                            if (radius > 0 && Math.Pow(radius - x - .5, 2) + Math.Pow(radius - y - .5, 2) > radius * radius)
                                continue;
                            if (!silhouette.IsVisible(fitted.Left + x + .5f, fitted.Top + y + .5f))
                                throw new Exception("Native material extends outside the painted corner.");
                        }
                }
            using (var image = new Bitmap(100, 60))
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Theme.Background);
                using (var brush = new SolidBrush(Theme.Surface))
                {
                    graphics.FillRectangle(brush, 5, 5, 30, 45);
                    graphics.FillRectangle(brush, 55, 5, 40, 45);
                }
                image.SetPixel(15, 15, Theme.TextColor);
                image.SetPixel(16, 15, Color.Red);
                image.SetPixel(17, 15, Color.FromArgb((Theme.Background.R + Theme.Border.R) / 2, (Theme.Background.G + Theme.Border.G) / 2, (Theme.Background.B + Theme.Border.B) / 2));
                using (var region = GlassFrame.Prepare(image))
                {
                    if (image.GetPixel(45, 20).A != 0 || region.IsVisible(45, 20))
                        throw new Exception("Glass must not fill gaps between surfaces.");
                    if (image.GetPixel(10, 10).A <= 0 || image.GetPixel(10, 10).A >= 255)
                        throw new Exception("Surface fill must be translucent.");
                    if (image.GetPixel(15, 15).ToArgb() != Theme.TextColor.ToArgb() || image.GetPixel(16, 15).ToArgb() != Color.Red.ToArgb())
                        throw new Exception("Text and icon pixels must retain full opacity and color.");
                    if (image.GetPixel(17, 15).A == 255)
                        throw new Exception("Antialiased edges must shed their opaque background matte.");
                    var surfaces = GlassFrame.Surfaces(region);
                    if (surfaces.Count != 2 || surfaces.Exists(rect => rect.Contains(45, 20)))
                        throw new Exception("Native blur bounds must stay within separated surfaces.");
                }
            }
            var nativeHandles = new List<IntPtr>();
            using (var form = new MainForm())
            {
                form.Show();
                form.SetPinned(true);
                Pump();
                if (form.Opacity != 1)
                    throw new Exception("Pin alone must not enable glass.");
                var pool = (GlassPool)typeof(PortableWindow).GetField("glassPool", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                var warm = System.Diagnostics.Stopwatch.StartNew();
                while (pool.ReadyCount < GlassPool.Capacity && warm.ElapsedMilliseconds < 5000)
                    Pump();
                if (pool.ReadyCount != GlassPool.Capacity)
                    throw new Exception("The bounded native reserve must finish warming while unlocked.");
                var setLock = typeof(PortableWindow).GetMethod("SetOverlayLocked", BindingFlags.Instance | BindingFlags.NonPublic);
                setLock.Invoke(form, new object[] { true });
                Pump();
                int ordinaryWindows = Application.OpenForms.Count;

                if (form.Opacity == 0)
                {
                    foreach (var surface in Surfaces(form))
                        nativeHandles.Add(surface.Handle);
                    var hit = WindowFromPoint(form.PointToScreen(new Point(200, 200)));
                    if (nativeHandles.Contains(hit))
                        throw new Exception("Locked glass must pass mouse input through.");
                    var positions = new Dictionary<LockedGlass.GlassWindow, Rectangle>();
                    Control grip = null;
                    foreach (var window in Surfaces(form))
                        positions.Add(window, window.Bounds);
                    foreach (Form owned in form.OwnedForms)
                    {
                        if (owned is UnlockWindow)
                            foreach (Control control in owned.Controls)
                                if (control.AccessibleName == "Move overlay")
                                    grip = control;
                    }
                    if (grip == null)
                        throw new Exception("The locked overlay must retain its drag grip.");
                    grip.Capture = true;
                    try
                    {
                        form.Location += new Size(12, 9);
                        foreach (var pair in positions)
                        {
                            var expected = pair.Value;
                            expected.Offset(12, 9);
                            if (pair.Key.Bounds != expected)
                                throw new Exception("Glass must follow a drag synchronously, without waiting for a render tick.");
                        }
                        form.Invalidate(true);
                        Pump();
                        if (!grip.Capture)
                            throw new Exception("Refreshing glass must not interrupt drag capture.");
                    }
                    finally { grip.Capture = false; }
                    typeof(MainForm).GetMethod("ToggleInlineMatrix", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                    Pump();
                    VerifyStack(form);
                    typeof(MainForm).GetMethod("ToggleInlineMatrix", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                    Pump();
                    VerifyStack(form);
                }
                setLock.Invoke(form, new object[] { false });
                Pump();
                if (nativeHandles.Exists(handle => IsWindowVisible(handle) && GlassNative.WindowBounds(handle).IntersectsWith(SystemInformation.VirtualScreen)))
                    throw new Exception("Unlock must remove every native glass surface from the desktop.");
                if (form.Opacity != 1 || !form.TopMost)
                    throw new Exception("Unlock must restore normal pinned content.");
                if (ordinaryWindows > 2 && Application.OpenForms.Count >= ordinaryWindows)
                    throw new Exception("Unlock must dispose glass companion windows.");
                if (pool.ReadyCount != GlassPool.Capacity)
                    throw new Exception("Unlock must return native surfaces for reuse without growing the reserve.");
                form.SetPinned(false);
            }
            if (nativeHandles.Exists(handle => IsWindow(handle)))
                throw new Exception("Closing the host must destroy pooled native handles.");
        }
        internal static IEnumerable<LockedGlass.GlassWindow> Surfaces(MainForm form)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var glass = typeof(PortableWindow).GetField("lockedGlass", flags).GetValue(form);
            foreach (var window in (IEnumerable<LockedGlass.GlassWindow>)typeof(LockedGlass).GetField("backdrops", flags).GetValue(glass))
                yield return window;
            yield return (LockedGlass.GlassWindow)typeof(LockedGlass).GetField("foreground", flags).GetValue(glass);
        }
        private static void VerifyStack(MainForm form)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var glass = typeof(PortableWindow).GetField("lockedGlass", flags).GetValue(form);
            var foreground = (LockedGlass.GlassWindow)typeof(LockedGlass).GetField("foreground", flags).GetValue(glass);
            var backdrops = (System.Collections.IEnumerable)typeof(LockedGlass).GetField("backdrops", flags).GetValue(glass);
            foreach (LockedGlass.GlassWindow backdrop in backdrops)
            {
                // Reproduce a native surface being raised after the foreground
                // was presented, including the final ally and matrix cells.
                GlassNative.Raise(backdrop.Handle);
                IntPtr above = backdrop.Handle;
                int remaining = 2000;
                while (above != IntPtr.Zero && above != foreground.Handle && remaining-- > 0)
                    above = GetWindow(above, 3);
                if (above != foreground.Handle)
                    throw new Exception("A glass surface occluded the sharp foreground.");
            }
        }
        private static void Pump()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 200)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(5);
            }
        }
    }
}
