using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class StartupTests
    {
        internal static void Run()
        {
            var themePreview = new object();
            try
            {
                using (var support = new SupportButton { Size = new Size(166, 40), SurroundColor = () => Theme.Surface })
                using (var image = new Bitmap(166, 40))
                    foreach (int mode in new[] { 2, 0, 2 })
                    {
                        Theme.PreviewAppearance(themePreview, mode);
                        support.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        if (image.GetPixel(0, 0).ToArgb() != Theme.Surface.ToArgb())
                            throw new Exception("Support button corners must follow the current card theme.");
                    }
            }
            finally { Theme.EndAppearancePreview(themePreview); }
            foreach (int closeMessage in new[] { 0x10, 0x112 })
                using (var host = new Form())
                using (var surface = new LockedGlass.GlassWindow())
                {
                    host.Show();
                    surface.CloseOwner = host;
                    NativeMethods.SendMessage(surface.Handle, closeMessage, new IntPtr(0xf060), IntPtr.Zero);
                    Application.DoEvents();
                    if (!host.IsDisposed)
                        throw new Exception("Closing an auxiliary glass surface must close its real host.");
                }
            using (var main = new MainForm())
            using (var child = new Form())
            using (var closeTimer = new Timer { Interval = 100 })
            {
                main.Show();
                int ticks = 0;
                closeTimer.Tick += (s, e) =>
                {
                    if (++ticks == 1)
                        NativeMethods.SendMessage(main.Handle, 0x112, new IntPtr(0xf060), IntPtr.Zero);
                    else if (ticks > 20)
                        child.Close();
                };
                closeTimer.Start();
                child.ShowDialog(main);
                Application.DoEvents();
                if (!main.IsDisposed || child.Visible)
                    throw new Exception("Taskbar close must close the main window and its modal child. Main disposed=" + main.IsDisposed + ", visible=" + main.Visible + ", enabled=" + main.Enabled + ", child visible=" + child.Visible + ", ticks=" + ticks);
            }
            using (var notes = new RichTextBox())
            {
                notes.Rtf = MarkdownRtf.Render("# Heading\n- **Bold** and `code`\n\n| Type | Result |\n| --- | ---: |\n| Fire | Better |\n\n<!-- hidden -->\n[Link](https://github.com)\n{literal}", 460);
                if (notes.Text.Contains("**") || notes.Text.Contains("hidden") || !notes.Text.Contains("Fire") || !notes.Text.Contains("Better") || !notes.Text.Contains("{literal}"))
                    throw new Exception("Markdown must render tables and inline formatting without leaking markup or comments.");
                notes.Select(notes.Text.IndexOf("Bold", StringComparison.Ordinal), 4);
                if (notes.SelectionFont == null || !notes.SelectionFont.Bold)
                    throw new Exception("Strong Markdown must produce bold text.");
            }
            using (var owner = new Form())
            using (var secondary = new Form())
            {
                owner.Show();
                secondary.Show(owner);
                int checks = 0;
                UpdatePrompt.OnStartup(owner, () => checks++);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < 1600)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                if (checks != 1)
                    throw new Exception("Startup must check once even with a secondary window open.");
            }
            using (var initialMain = new MainForm())
            {
                initialMain.Show();
                Application.DoEvents();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var initialSelection = (Selection)typeof(MainForm).GetField("selection", flags).GetValue(initialMain);
                initialSelection.Main = Array.IndexOf(Model.Names, "Grass");
                initialSelection.Sub = Array.IndexOf(Model.Names, "Light");
                typeof(MainForm).GetMethod("RefreshSelection", flags).Invoke(initialMain, null);
                typeof(MainForm).GetField("hovered", flags).SetValue(initialMain, -1);
                var buttons = (IconButton[])typeof(MainForm).GetField("types", flags).GetValue(initialMain);
                buttons[Array.IndexOf(Model.Names, "Water")].Focus();
                buttons[Array.IndexOf(Model.Names, "Dark")].Focus();
                var preview = (Selection)typeof(MainForm).GetProperty("preview", flags).GetValue(initialMain, null);
                if (preview.Main != initialSelection.Main || preview.Sub != initialSelection.Sub)
                    throw new Exception("Initial keyboard focus must not override selected types");
            }

        }
    }
}
