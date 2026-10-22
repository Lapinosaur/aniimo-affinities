using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class BusyPreview
    {
        internal static void Save(string variant, string path)
        {
            using (PortableWindow form = variant == "restart" ? (PortableWindow)new UpdateWindow(new ReleaseUpdate { Version = "next" }) : new SettingsDialog(KeyBindings.Defaults()))
            {
                form.Show();
                Application.DoEvents();
                if (variant == "restart")
                {
                    var button = form.Controls.OfType<SettingsButton>().Single(x => x.Text == "Download");
                    button.BeginBusy("Restarting…");
                    form.Controls.OfType<Label>().Single(x => x.Text.StartsWith("Download now")).Text = "Preparing the restart… Please wait.";
                }
                else
                {
                    form.Controls.OfType<SettingsButton>().Single(x => x.AccessibleName == "About & Credits").PerformClick();
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    while (clock.ElapsedMilliseconds < 250)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(5);
                    }
                    var button = Find(form);
                    if (button == null)
                        throw new Exception("Update action missing from preview.");
                    button.BeginBusy("Checking…");
                }
                Application.DoEvents();
                using (var image = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                    image.Save(path);
                }
            }
        }
        private static SettingsButton Find(Control control)
        {
            var button = control as SettingsButton;
            if (button != null && button.Text == "Check for updates")
                return button;
            foreach (Control child in control.Controls)
            {
                var found = Find(child);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
