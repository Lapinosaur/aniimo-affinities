using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class RadarTests
    {
        internal static void Run()
        {
            if (Math.Abs(RadarRenderer.RadiusFraction(1) - 1 / 2.56) > 0.000001 || RadarRenderer.RadiusFraction(2.56) != 1 || RadarRenderer.RadiusFraction(0) != 0)
                throw new Exception("Radar scale mismatch");
            using (var radar = new RadarDialog(0, 1, KeyBindings.Defaults()))
            {
                radar.Show();
                Application.DoEvents();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var selected = (Selection)typeof(RadarDialog).GetField("selection", flags).GetValue(radar);
                if (selected.Main != 0 || selected.Sub != 1)
                    throw new Exception("Radar lost dual selection");
                ((IconButton)typeof(RadarDialog).GetField("separateButton", flags).GetValue(radar)).PerformClick();
                if ((bool)typeof(RadarDialog).GetField("combined", flags).GetValue(radar))
                    throw new Exception("Radar separate view failed");
                ((IconButton)typeof(RadarDialog).GetField("combinedButton", flags).GetValue(radar)).PerformClick();
                if (!(bool)typeof(RadarDialog).GetField("combined", flags).GetValue(radar))
                    throw new Exception("Radar combined view failed");
            }
        }
    }
}
