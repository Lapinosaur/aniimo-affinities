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
            float compact = RadarRenderer.FitRadius(new SizeF(440, 220));
            if (compact < 59 || RadarRenderer.FitRadius(new SizeF(440, 300)) <= compact || RadarRenderer.FitRadius(new SizeF(300, 300)) >= RadarRenderer.FitRadius(new SizeF(440, 300)))
                throw new Exception("Radar radius must adapt to both dimensions of the available content.");
            if (Math.Abs(RadarRenderer.RadiusFraction(1) - 1 / 2.56) > 0.000001 || RadarRenderer.RadiusFraction(2.56) != 1 || RadarRenderer.RadiusFraction(0) != 0)
                throw new Exception("Radar scale mismatch");
            for (int main = 0; main < 9; main++)
                for (int sub = -1; sub < 9; sub++)
                {
                    var selection = new Selection { Main = main, Sub = sub };
                    var values = Model.Factors(main, sub);
                    double maximum = RadarRenderer.SharedMaximum(selection);
                    if (Math.Abs(maximum - values.Max()) > .000001 || RadarRenderer.RadiusFraction(values.Max(), maximum) != 1)
                        throw new Exception("Paired radars must use their common data maximum");
                    if (values.Max() < 1.61 && maximum > 1.61)
                        throw new Exception("Single-scale radar retained the unused 2.56 ring");
                }
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
