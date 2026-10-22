using System;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class UiFonts
    {
        private sealed class OwnedFont
        {
            internal Font Value;
        }
        private static readonly ConditionalWeakTable<Control, OwnedFont> owned = new ConditionalWeakTable<Control, OwnedFont>();

        // Adopt only a font created for this control, never an inherited/system/shared font.
        internal static T Own<T>(T control) where T : Control
        {
            Assign(control, control.Font);
            return control;
        }
        internal static void Assign(Control control, Font font)
        {
            OwnedFont owner;
            if (!owned.TryGetValue(control, out owner))
            {
                owner = new OwnedFont();
                owned.Add(control, owner);
                control.Disposed += Release;
            }
            var previous = owner.Value;
            try
            {
                control.Font = font;
            }
            catch { if (!ReferenceEquals(previous, font)) font.Dispose(); throw; }
            if (!ReferenceEquals(control.Font, font))
            {
                if (!ReferenceEquals(previous, font))
                    font.Dispose();
                return;
            }
            owner.Value = font;
            if (previous != null && !ReferenceEquals(previous, font))
                previous.Dispose();
        }
        private static void Release(object sender, EventArgs args)
        {
            var control = (Control)sender;
            OwnedFont owner;
            if (owned.TryGetValue(control, out owner))
            {
                owned.Remove(control);
                control.Disposed -= Release;
                if (owner.Value != null)
                    owner.Value.Dispose();
            }
        }
        internal static Font Create(string family, float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(family, size * DisplaySettings.FontScale, style);
        }
        internal static Font Create(FontFamily family, float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(family, size * DisplaySettings.FontScale, style);
        }
    }
}
