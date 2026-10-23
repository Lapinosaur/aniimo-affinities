using System;
using System.Drawing;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class InteractiveGlassPreview
    {
        internal static void Save(string mode, string path)
        {
            TransparencyPreferences.Save(true, true, true);
            using (var background = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(50, 50, 1100, 800) })
            using (PortableWindow window = mode == "team-glass-main" ? (PortableWindow)new MainForm() : new TransparencyDialog(() => { }))
            {
                background.Paint += (s, e) =>
                {
                    for (int x = 0; x < background.Width; x += 40)
                        for (int y = 0; y < background.Height; y += 40)
                            using (var brush = new SolidBrush((x / 40 + y / 40) % 2 == 0 ? Color.FromArgb(70, 150, 170) : Color.FromArgb(160, 90, 145)))
                                e.Graphics.FillRectangle(brush, x, y, 40, 40);
                };
                background.TopMost = true;
                background.Show();
                window.TopMost = true;
                window.StartPosition = FormStartPosition.Manual;
                window.Location = new Point(100, 100);
                window.Show(background);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < 700)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                using (var bitmap = new Bitmap(window.Width, window.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(window.Location, Point.Empty, window.Size);
                    bitmap.Save(path);
                }
            }
        }
    }
}
