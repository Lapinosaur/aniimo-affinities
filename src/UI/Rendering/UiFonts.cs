using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class UiFonts
    {
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
