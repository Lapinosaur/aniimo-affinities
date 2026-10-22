using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class DisplayRollbackTests
    {
        internal static void Run()
        {
            using (var origin = new MainForm())
            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            using (var display = new DisplayDialog())
            using (var probe = new Timer { Interval = 40 })
            {
                origin.Show();
                settings.Show(origin);
                typeof(DisplayDialog).GetField("scale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(display, DisplaySettings.FontScale == 1.1f ? 0.9f : 1.1f);
                int phase = 0;
                Exception failed = null;
                probe.Tick += (s, e) =>
                {
                    try
                    {
                        if (phase == 0)
                        {
                            phase = 1;
                            display.Controls.OfType<SettingsButton>().Single(button => button.Text == "Save").PerformClick();
                        }
                        else if (phase == 1)
                        {
                            var confirmation = Application.OpenForms.OfType<DisplayConfirmationDialog>().FirstOrDefault();
                            if (confirmation == null)
                                return;
                            if (display.Visible || settings.Visible || !origin.Visible)
                                throw new Exception("Display preview did not reveal origin");
                            phase = 2;
                            confirmation.Controls.OfType<SettingsButton>().Single(button => button.Text == "Revert").PerformClick();
                        }
                        else
                        {
                            if (!display.Visible || !settings.Visible)
                                throw new Exception("Settings were not restored after display preview");
                            probe.Stop();
                            display.Close();
                        }
                    }
                    catch (Exception error) { failed = error; probe.Stop(); foreach (var confirmation in Application.OpenForms.OfType<DisplayConfirmationDialog>().ToArray()) confirmation.Close(); display.Close(); }
                };
                probe.Start();
                display.ShowDialog(settings);
                if (failed != null)
                    throw failed;
            }
            float savedScale = DisplaySettings.FontScale;
            bool savedBadges = DisplaySettings.ShowBadges;
            try
            {
                DisplaySettings.ApplyDisplay(1.1f, false);
                using (var confirmation = new DisplayConfirmationDialog(0.1))
                    if (confirmation.ShowDialog() != DialogResult.Cancel)
                        throw new Exception("Display timeout must revert");
            }
            finally { DisplaySettings.ApplyDisplay(savedScale, savedBadges); }
            if (DisplaySettings.FontScale != savedScale || DisplaySettings.ShowBadges != savedBadges)
                throw new Exception("Display rollback failed");
        }
    }
}
