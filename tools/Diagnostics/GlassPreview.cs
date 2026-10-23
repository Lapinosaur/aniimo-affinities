using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class GlassPreview
    {
        internal static void Save(string path, string mode)
        {
            using (var background = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(80, 60, 1080, 800) })
            using (var form = new MainForm())
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                if (mode == "radar")
                    typeof(MainForm).GetMethod("ToggleInlineRadar", flags).Invoke(form, null);
                if (mode == "matrix")
                    typeof(MainForm).GetMethod("ToggleInlineMatrix", flags).Invoke(form, null);
                if (mode != "radar")
                {
                    var stored = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(form);
                    var sample = TeamTests.Sample();
                    for (int i = 0; i < 6; i++)
                        stored.Allies[i] = sample.Allies[i];
                    stored.Visible = true;
                    stored.Position = "Bottom";
                    typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(form, null);
                }
                background.Paint += (s, e) =>
                {
                    for (int x = 0; x < background.Width; x += 20)
                        for (int y = 0; y < background.Height; y += 20)
                            using (var brush = new SolidBrush((x / 20 + y / 20) % 2 == 0 ? Color.FromArgb(70, 150, 170) : Color.FromArgb(160, 90, 145)))
                                e.Graphics.FillRectangle(brush, x, y, 20, 20);
                };
                background.TopMost = true;
                background.Show();
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(110, 90);
                form.Show();
                form.Location = new Point(110, 90);
                background.Bounds = new Rectangle(form.Left - 30, form.Top - 30, form.Width + 60, form.Height + 60);
                var activation = System.Diagnostics.Stopwatch.StartNew();
                typeof(PortableWindow).GetMethod("SetOverlayLocked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(form, new object[] { true });
                Console.WriteLine("Glass activation: " + activation.Elapsed.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " ms");
                var settle = System.Diagnostics.Stopwatch.StartNew();
                while (settle.ElapsedMilliseconds < 1800)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
                // Other topmost apps may activate while the preview settles.
                // Put the diagnostic backdrop below its subject for capture.
                GlassNative.Raise(background.Handle);
                foreach (var window in GlassTests.Surfaces(form))
                    GlassNative.Raise(window.Handle);
                foreach (Form window in form.OwnedForms)
                    if (window is UnlockWindow)
                        GlassNative.Raise(window.Handle);
                using (var image = new Bitmap(form.Width, form.Height))
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.CopyFromScreen(form.Location, Point.Empty, form.Size);
                    image.Save(path);
                }
                Console.WriteLine("Glass source opacity: " + form.Opacity);
                Application.DoEvents();
                System.Threading.Thread.Sleep(100);
                using (var image = new Bitmap(background.Width, background.Height))
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.CopyFromScreen(background.Location, Point.Empty, background.Size);
                    image.Save(System.IO.Path.ChangeExtension(path, ".context.png"));
                }
                typeof(PortableWindow).GetMethod("SetOverlayLocked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(form, new object[] { false });
                if (form.Opacity != 1)
                    throw new Exception("Unlock must restore ordinary content.");
            }
        }
    }
}
