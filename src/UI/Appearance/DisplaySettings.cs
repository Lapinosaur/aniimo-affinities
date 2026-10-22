using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class DisplaySettings
    {

        internal static event Action DisplayChanged;
        internal static float FontScale = 1;
        internal static float WindowScale = 1;
        internal static bool ShowBadges = true;
        private static readonly string DisplayPath = PreferenceFiles.FilePath("display.txt");
        internal static void SaveDisplay(float scale, bool badges, float windowScale = 1)
        {
            PreferenceFiles.WriteLines(DisplayPath, new[] { scale.ToString(System.Globalization.CultureInfo.InvariantCulture), badges.ToString(), windowScale.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            ApplyDisplay(scale, badges, windowScale);
        }
        internal static void ApplyDisplay(float scale, bool badges, float windowScale = 1)
        {
            float nextZoom = Math.Max(.5f, Math.Min(1.5f, windowScale));
            bool zoomChanged = Math.Abs(nextZoom - WindowScale) > .0001f;
            bool fontChanged = Math.Abs(scale - FontScale) > .0001f;
            bool badgesChanged = badges != ShowBadges;
            if (!zoomChanged && !fontChanged && !badgesChanged)
                return;
            WindowScale = nextZoom;
            if (zoomChanged)
                foreach (Form form in Application.OpenForms)
                {
                    var window = form as PortableWindow;
                    if (window != null)
                        window.ApplyWindowScale();
                }
            float ratio = scale / FontScale;
            FontScale = scale;
            ShowBadges = badges;
            if (fontChanged)
            {
                // Snapshot before changing a parent, so inherited fonts are scaled only once.
                var fonts = new System.Collections.Generic.List<Tuple<Control, Font>>();
                foreach (Form form in Application.OpenForms)
                    CollectFonts(form, ratio, fonts);
                foreach (var pair in fonts)
                    UiFonts.Assign(pair.Item1, pair.Item2);
            }
            if (fontChanged || badgesChanged)
                foreach (Form form in Application.OpenForms)
                    form.Invalidate(true);
            if (DisplayChanged != null)
                DisplayChanged();
        }
        private static void CollectFonts(Control control, float ratio, System.Collections.Generic.List<Tuple<Control, Font>> fonts)
        {
            fonts.Add(Tuple.Create(control, new Font(control.Font.FontFamily, control.Font.Size * ratio, control.Font.Style)));
            foreach (Control child in control.Controls)
                CollectFonts(child, ratio, fonts);
        }
        internal static void Load()
        {
            try
            {
                var display = System.IO.File.ReadAllLines(DisplayPath);
                float scale;
                bool badges;
                if (display.Length >= 2 && float.TryParse(display[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out scale) && scale >= 0.9f && scale <= 1.1f && bool.TryParse(display[1], out badges))
                {
                    FontScale = scale;
                    ShowBadges = badges;
                }
                float zoom;
                if (display.Length >= 3 && float.TryParse(display[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out zoom) && zoom >= .5f && zoom <= 1.5f)
                    WindowScale = zoom;
            }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
