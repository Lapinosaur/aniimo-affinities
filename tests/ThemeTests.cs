using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ThemeTests
    {
        internal static void Run()
        {
            int originalAppearance = Theme.Appearance;
            foreach (int mode in new[] { 0, 2 })
            {
                Theme.Appearance = mode;
                Theme.ApplyAppearance();
                foreach (double multiplier in new[] { .390625, .625, 1, 1.6, 2.56 })
                    if (Contrast(Theme.MultiplierInk(multiplier), Theme.MultiplierFill(multiplier)) < 4.5)
                        throw new Exception("Matrix multiplier contrast below 4.5:1");
                foreach (var role in new[] { Theme.Attack, Theme.Defense })
                    if (Contrast(role, Theme.Surface) < 4.5 || Contrast(role, Theme.Background) < 4.5)
                        throw new Exception("Attack/defense contrast below 4.5:1");
                foreach (var pair in new[] { new[] { Theme.TextColor, Theme.HoverSurface }, new[] { Theme.AccentForeground, Theme.AccentHover }, new[] { Theme.TextColor, Theme.Surface }, new[] { Theme.TextColor, Theme.Border }, new[] { Theme.Muted, Theme.Background }, new[] { Theme.AccentForeground, Theme.AccentSurface }, new[] { Theme.AccentInk, Theme.Accent }, new[] { Theme.AccentInk, Theme.AccentPressed } })
                    if (Contrast(pair[0], pair[1]) < 4.5)
                        throw new Exception("Theme text contrast below 4.5:1: mode=" + mode + " ink=" + pair[0] + " fill=" + pair[1]);
            }
            Theme.Appearance = originalAppearance;
            Theme.ApplyAppearance();
            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            using (var unrelated = new Form())
            {
                if (settings.KeyboardNavigation)
                    throw new Exception("Navigation badges must start hidden");
                var input = Message.Create(settings.Handle, 0x100, new IntPtr((int)Keys.Tab), IntPtr.Zero);
                if (settings.PreFilterMessage(ref input) || !settings.KeyboardNavigation)
                    throw new Exception("Keyboard navigation not detected");
                input = Message.Create(unrelated.Handle, 0x201, IntPtr.Zero, IntPtr.Zero);
                settings.PreFilterMessage(ref input);
                if (!settings.KeyboardNavigation)
                    throw new Exception("Unrelated window changed navigation mode");
                input = Message.Create(settings.Handle, 0x201, IntPtr.Zero, IntPtr.Zero);
                if (settings.PreFilterMessage(ref input) || settings.KeyboardNavigation)
                    throw new Exception("Mouse input did not hide navigation badges");
            }
        }
        private static double Contrast(Color first, Color second)
        {
            Func<Color, double> luminance = color =>
            {
                Func<byte, double> channel = value => value / 255.0 <= 0.04045 ? value / 255.0 / 12.92 : Math.Pow((value / 255.0 + 0.055) / 1.055, 2.4);
                return 0.2126 * channel(color.R) + 0.7152 * channel(color.G) + 0.0722 * channel(color.B);
            };
            double a = luminance(first), b = luminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }
    }
}
