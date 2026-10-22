using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class SettingsTests
    {
        internal static void Run()
        {
            int previewMain = Array.IndexOf(Model.Names, "Fire");
            var previewKeys = KeyBindings.Defaults();
            using (var displayPreview = new DisplayDialog(null, () => previewMain, () => previewKeys))
            {
                var choices = displayPreview.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<DisplayChoice>()).Where(choice => choice.BadgePreview).ToArray();
                if (choices.Length != 2 || choices.Any(choice => choice.PreviewType() != previewMain))
                    throw new Exception("Badge previews must use selected Main");
                previewMain = Array.IndexOf(Model.Names, "Water");
                if (choices.Any(choice => choice.PreviewType() != previewMain))
                    throw new Exception("Badge previews did not follow Main change");
                previewKeys[Array.IndexOf(KeyBindings.Actions, "Water")] = Keys.Q;
                if (choices.Any(choice => choice.PreviewBindings()[Array.IndexOf(KeyBindings.Actions, "Water")] != Keys.Q))
                    throw new Exception("Badge previews must use draft rebinds");
                previewMain = -1;
                int fallback = choices[0].PreviewType();
                if (fallback < 0 || fallback >= Model.Names.Length || choices.Any(choice => choice.PreviewType() != fallback))
                    throw new Exception("Empty Main requires one stable shared fallback");
            }

            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            {
                settings.Show();
                Application.DoEvents();
                int initial = settings.ClientSize.Height;
                settings.Controls.OfType<SettingsButton>().Single(button => button.Text == "Window & overlay").PerformClick();
                if (settings.ClientSize.Height != initial)
                    throw new Exception("Tab height should animate instead of jumping");
                settings.Controls.OfType<SettingsButton>().Single(button => button.Text == "Display").PerformClick();
                var transition = System.Diagnostics.Stopwatch.StartNew();
                while (transition.ElapsedMilliseconds < 260)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                if (settings.ClientSize.Height != 594)
                    throw new Exception("Rapid tab changes must settle at latest height");
                settings.Controls.OfType<SettingsButton>().Single(button => button.Text == "Types").PerformClick();
                transition.Restart();
                while (transition.ElapsedMilliseconds < 260)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                if (settings.ClientSize.Height != initial)
                    throw new Exception("Types height was not restored after animation");
            }

        }
    }
}
