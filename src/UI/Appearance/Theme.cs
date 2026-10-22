using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class Theme
    {
        internal const float WindowRadius = 16, PanelRadius = 16, SelectorRadius = 12, ControlRadius = 8;
        internal static readonly Color Anthracite = Color.FromArgb(32, 39, 53), Ivory = Color.FromArgb(245, 241, 232);
        internal static Color AccentSurface, AccentHover, AccentPressed, AccentLight, AccentInk;
        internal static Color Background = Anthracite, Surface = Anthracite, Border = Anthracite, Accent = Color.FromArgb(25, 197, 199), Muted = Ivory;
        internal static Color TextColor = Ivory;
        // Opaque recessed content must never share the window transparency key.
        internal static Color InsetSurface
        {
            get
            {
                return Color.FromArgb(Background.R, Background.G, Background.B + 1);
            }
        }
        internal static Color Attack, Defense, AttackSurface, DefenseSurface, ReducedDamage, IncreasedDamage;
        internal static Color AccentForeground = Accent, HoverSurface = Anthracite, StrongBorder = Anthracite, Error = Color.FromArgb(245, 105, 115), TooltipShadow = Anthracite;
        internal static int Appearance = 1;
        internal static int Revision;
        internal static event Action Changed;
        private static readonly string AppearancePath = PreferenceFiles.FilePath("appearance.txt");
        internal static void LoadAppearance()
        {
            DisplaySettings.Load();
            int mode;
            try
            {
                if (int.TryParse(System.IO.File.ReadAllText(AppearancePath), out mode) && mode >= 0 && mode <= 2)
                    Appearance = mode;
            }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            ApplyAppearance();
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (Appearance != 1 || Application.OpenForms.Count == 0)
                    return;
                var form = Application.OpenForms[0];
                if (!form.IsDisposed && form.IsHandleCreated)
                    form.BeginInvoke(new Action(ApplyAppearance));
            };
        }
        internal static void CycleAppearance()
        {
            int next = (Appearance + 1) % 3;
            try
            {
                PreferenceFiles.WriteText(AppearancePath, next.ToString());
            }
            catch (System.IO.IOException) { return; }
            catch (UnauthorizedAccessException) { return; }
            Appearance = next;
            ApplyAppearance();
        }
        internal static void ApplyAppearance()
        {
            bool light = Appearance == 2;
            if (Appearance == 1)
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    light = key == null || Convert.ToInt32(key.GetValue("AppsUseLightTheme", 1)) != 0;
            }
            var oldBackground = Background;
            var oldSurface = Surface;
            var oldText = TextColor;
            var oldMuted = Muted;
            var oldAccentSurface = AccentSurface;
            var oldAccentHover = AccentHover;
            var oldError = Error;
            var oldBorder = Border;
            Background = light ? Ivory : Anthracite;
            Surface = Blend(Background, light ? Anthracite : Ivory, light ? 0.025f : 0.04f);
            TextColor = light ? Anthracite : Ivory;
            Border = Blend(Background, TextColor, 0.22f);
            Muted = Blend(Background, TextColor, light ? 0.72f : 0.68f);
            AccentSurface = Blend(Surface, Accent, light ? 0.14f : 0.18f);
            AccentHover = Blend(Surface, Accent, light ? 0.26f : 0.18f);
            AccentPressed = Blend(Accent, Anthracite, 0.16f);
            AccentLight = Blend(Accent, Ivory, 0.22f);
            AccentInk = Anthracite;
            AccentForeground = light ? Blend(Accent, Anthracite, 0.68f) : Accent;
            HoverSurface = Blend(Background, TextColor, 0.09f);
            StrongBorder = Blend(Background, TextColor, 0.52f);
            Error = light ? Color.FromArgb(177, 35, 55) : Color.FromArgb(245, 105, 115);
            TooltipShadow = Blend(Background, Anthracite, 0.26f);
            Attack = light ? Color.FromArgb(151, 70, 18) : Color.FromArgb(255, 183, 112);
            Defense = light ? Color.FromArgb(35, 91, 156) : Color.FromArgb(131, 188, 255);
            AttackSurface = Blend(Surface, Attack, 0.10f);
            DefenseSurface = Blend(Surface, Defense, 0.10f);
            ReducedDamage = light ? Color.FromArgb(105, 53, 140) : Color.FromArgb(212, 175, 245);
            IncreasedDamage = light ? AccentForeground : Blend(Accent, TextColor, .22f);
            foreach (Form form in Application.OpenForms)
            {
                ApplyControlTheme(form, oldBackground, oldSurface, oldText, oldMuted);
                UpdateAccentControls(form, oldAccentSurface, oldAccentHover, oldError, oldBorder);
                if (form is PortableWindow && form.TopMost)
                    form.TransparencyKey = Background;
            }
            Revision++;
            if (Changed != null)
                Changed();
        }
        internal static Color MultiplierInk(double value)
        {
            return value < .999 ? ReducedDamage : value > 1.001 ? IncreasedDamage : Muted;
        }
        internal static Color MultiplierFill(double value)
        {
            if (value >= .999 && value <= 1.001)
                return Surface;
            bool extreme = value < .5 || value > 2;
            return Blend(Surface, value < 1 ? ReducedDamage : Accent, extreme ? .21f : .12f);
        }
        internal static Color MultiplierBorder(double value)
        {
            return value >= .999 && value <= 1.001 ? Border : Blend(Surface, MultiplierInk(value), value < .5 || value > 2 ? .55f : .32f);
        }
        internal static Color BrandSurface(Color brand, bool hover)
        {
            return Blend(Surface, brand, hover ? .18f : .10f);
        }
        internal static Color BrandInk(Color brand, Color background)
        {
            Color ink = brand;
            for (int i = 0; i < 10 && Contrast(ink, background) < 3; i++)
                ink = Blend(ink, TextColor, .2f);
            return ink;
        }
        private static double Contrast(Color a, Color b)
        {
            Func<Color, double> luminance = color =>
            {
                Func<byte, double> channel = value => value / 255.0 <= .04045 ? value / 255.0 / 12.92 : Math.Pow((value / 255.0 + .055) / 1.055, 2.4);
                return .2126 * channel(color.R) + .7152 * channel(color.G) + .0722 * channel(color.B);
            };
            double first = luminance(a), second = luminance(b);
            return (Math.Max(first, second) + .05) / (Math.Min(first, second) + .05);
        }
        private static Color Blend(Color background, Color foreground, float amount)
        {
            return Color.FromArgb((int)(background.R + (foreground.R - background.R) * amount), (int)(background.G + (foreground.G - background.G) * amount), (int)(background.B + (foreground.B - background.B) * amount));
        }
        private static void UpdateAccentControls(Control control, Color oldSurface, Color oldHover, Color oldError, Color oldBorder)
        {
            if (control.BackColor == oldSurface)
                control.BackColor = AccentSurface;
            else if (control.BackColor == oldBorder)
                control.BackColor = Border;
            if (control.ForeColor == oldError)
                control.ForeColor = Error;
            var button = control as Button;
            if (button != null && button.FlatAppearance.MouseOverBackColor == oldHover)
                button.FlatAppearance.MouseOverBackColor = AccentHover;
            foreach (Control child in control.Controls)
                UpdateAccentControls(child, oldSurface, oldHover, oldError, oldBorder);
        }
        private static void ApplyControlTheme(Control control, Color oldBackground, Color oldSurface, Color oldText, Color oldMuted)
        {
            if (control.BackColor == oldBackground)
                control.BackColor = Background;
            else if (control.BackColor == oldSurface)
                control.BackColor = Surface;
            if (control.ForeColor == oldText)
                control.ForeColor = TextColor;
            else if (control.ForeColor == oldMuted)
                control.ForeColor = Muted;
            foreach (Control child in control.Controls)
                ApplyControlTheme(child, oldBackground, oldSurface, oldText, oldMuted);
            control.Invalidate();
        }
        // Inputs: rectangle and radius. Output: rounded vector path. Fallback: none. Raises: invalid geometry.
    }
}
