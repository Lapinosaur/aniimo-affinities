using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class WindowBoundsTests
    {
        internal static void Run()
        {
            var monitor = new Rectangle(0, 0, 1920, 1040);
            if (PortableWindow.KeepCaptionVisible(new Rectangle(100, -200, 860, 500), monitor, 54).Top != 0)
                throw new Exception("Caption escaped above monitor");
            if (PortableWindow.KeepCaptionVisible(new Rectangle(100, 1100, 860, 500), monitor, 54).Top != 986)
                throw new Exception("Caption escaped below monitor");
            var second = new Rectangle(1920, -300, 1920, 1400);
            var crossing = new Rectangle(1800, -100, 860, 500);
            if (PortableWindow.KeepCaptionVisible(crossing, second, 54) != crossing)
                throw new Exception("Cross-monitor movement blocked");
            var leftMonitor = new Rectangle(-1920, 200, 1920, 1040);
            if (PortableWindow.KeepCaptionVisible(new Rectangle(-1500, 0, 860, 500), leftMonitor, 54).Top != 200)
                throw new Exception("Offset monitor caption escaped");
        }
    }
}
