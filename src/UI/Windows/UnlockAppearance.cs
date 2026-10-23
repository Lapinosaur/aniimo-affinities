using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace AniimoAffinities
{
    // Keep native controls as input targets while compositing each button independently.
    internal sealed class UnlockAppearance : NativeWindow, IDisposable
    {
        private readonly UnlockWindow source;
        private readonly LockedGlass.GlassWindow surface = new LockedGlass.GlassWindow();
        private readonly Timer fadeTimer = new Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        private readonly System.Collections.Generic.Dictionary<Control, Fade> fades = new System.Collections.Generic.Dictionary<Control, Fade>();
        internal sealed class Fade
        {
            private float from, target;
            private double started;
            internal bool Active
            {
                get; private set;
            }
            internal Fade(float value)
            {
                from = target = value;
            }
            internal float Sample(float next, double now)
            {
                float progress = (float)Math.Max(0, Math.Min(1, (now - started) / 180));
                float eased = progress * progress * (3 - 2 * progress);
                float value = progress >= 1 ? target : from + (target - from) * eased;
                if (next != target)
                {
                    from = value;
                    target = next;
                    started = now;
                }
                Active = value != target;
                return value;
            }
        }
        private bool queued, rendering, disposed, failed;
        internal UnlockAppearance(UnlockWindow window)
        {
            source = window;
            AssignHandle(window.Handle);
            foreach (Control control in source.Controls)
            {
                fades.Add(control, new Fade(source.ButtonOpacity(control)));
                control.Invalidated += Invalidated;
            }
            fadeTimer.Tick += (s, e) => Queue();
            Render();
        }
        private void Invalidated(object sender, InvalidateEventArgs e)
        {
            Queue();
        }
        internal void Queue()
        {
            if (disposed || queued || rendering || failed || !source.IsHandleCreated)
                return;
            queued = true;
            source.BeginInvoke(new Action(() => { queued = false; if (!disposed) Render(); }));
        }
        internal static Bitmap Compose(UnlockWindow window)
        {
            return Compose(window, window.ButtonOpacity);
        }
        private static Bitmap Compose(UnlockWindow window, Func<Control, float> opacity)
        {
            var image = new Bitmap(window.Width, window.Height);
            using (var graphics = Graphics.FromImage(image))
            {
                if (window.Region != null)
                    graphics.SetClip(window.Region, System.Drawing.Drawing2D.CombineMode.Replace);
                foreach (Control control in window.Controls)
                {
                    if (!control.Visible)
                        continue;
                    using (var button = new Bitmap(control.Width, control.Height))
                    using (var attributes = new ImageAttributes())
                    {
                        control.DrawToBitmap(button, new Rectangle(Point.Empty, button.Size));
                        var matrix = new ColorMatrix();
                        matrix.Matrix33 = opacity(control);
                        attributes.SetColorMatrix(matrix);
                        graphics.DrawImage(button, control.Bounds, 0, 0, button.Width, button.Height, GraphicsUnit.Pixel, attributes);
                    }
                }
            }
            return image;
        }
        private void Render()
        {
            if (disposed || failed || rendering)
                return;
            var owner = source.Owner as PortableWindow;
            if (!source.Visible || owner != null && owner.TutorialSnapshot)
            {
                surface.Hide();
                fadeTimer.Stop();
                return;
            }
            rendering = true;
            try
            {
                bool animating = false;
                double now = clock.Elapsed.TotalMilliseconds;
                using (var image = Compose(source, control =>
                {
                    var fade = fades[control];
                    float value = fade.Sample(source.ButtonOpacity(control), now);
                    animating |= fade.Active;
                    return value;
                }))
                    if (!GlassNative.Present(surface.Handle, image, source.Location))
                    {
                        failed = true;
                        fadeTimer.Stop();
                        surface.Hide();
                        source.Opacity = 1;
                        return;
                    }
                fadeTimer.Enabled = animating;
                source.Opacity = 1.0 / 255;
                Position();
            }
            finally { rendering = false; }
        }
        private void Position()
        {
            if (disposed || failed)
                return;
            if (!source.Visible)
            {
                surface.Hide();
                return;
            }
            surface.Bounds = source.Bounds;
            surface.Show();
            GlassNative.Behind(surface.Handle, source.Handle);
        }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == 0x47)
                Position();
        }
        public void Dispose()
        {
            disposed = true;
            fadeTimer.Dispose();
            foreach (Control control in source.Controls)
                control.Invalidated -= Invalidated;
            ReleaseHandle();
            surface.Dispose();
        }
    }
}
