using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class RegressionTests
    {
        internal static void Run()
        {
            Console.WriteLine("Resources");
            ResourceTests.Run();
            Console.WriteLine("Packaging");
            PackagingTests.Run();
            Console.WriteLine("PreferenceFiles");
            PreferenceFilesTests.Run();
            Console.WriteLine("Tutorial");
            TutorialTests.Run();
            Console.WriteLine("Startup");
            StartupTests.Run();
            Console.WriteLine("Session");
            SessionTests.Run();
            Console.WriteLine("Settings");
            SettingsTests.Run();
            Console.WriteLine("HoldControls");
            HoldControlsTests.Run();
            Console.WriteLine("Zoom");
            ZoomTests.Run();
            Console.WriteLine("ViewSwitching");
            ViewSwitchingTests.Run();
            Console.WriteLine("DisplayRollback");
            DisplayRollbackTests.Run();
            Console.WriteLine("WindowAttention");
            WindowAttentionTests.Run();
            Console.WriteLine("Radar");
            RadarTests.Run();
            Console.WriteLine("Matrix");
            MatrixTests.Run();
            Console.WriteLine("Theme");
            ThemeTests.Run();
            Console.WriteLine("WindowBounds");
            WindowBoundsTests.Run();
            Console.WriteLine("Keyboard");
            KeyboardTests.Run();
            Console.WriteLine("Matchups");
            MatchupsTests.Run();
        }
    }
}
