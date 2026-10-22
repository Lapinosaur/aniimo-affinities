using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class StartupTests
    {
        internal static void Run()
        {
            using (var initialMain = new MainForm())
            {
                initialMain.Show();
                Application.DoEvents();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var initialSelection = (Selection)typeof(MainForm).GetField("selection", flags).GetValue(initialMain);
                initialSelection.Main = Array.IndexOf(Model.Names, "Grass");
                initialSelection.Sub = Array.IndexOf(Model.Names, "Light");
                typeof(MainForm).GetMethod("RefreshSelection", flags).Invoke(initialMain, null);
                typeof(MainForm).GetField("hovered", flags).SetValue(initialMain, -1);
                var buttons = (IconButton[])typeof(MainForm).GetField("types", flags).GetValue(initialMain);
                buttons[Array.IndexOf(Model.Names, "Water")].Focus();
                buttons[Array.IndexOf(Model.Names, "Dark")].Focus();
                var preview = (Selection)typeof(MainForm).GetProperty("preview", flags).GetValue(initialMain, null);
                if (preview.Main != initialSelection.Main || preview.Sub != initialSelection.Sub)
                    throw new Exception("Initial keyboard focus must not override selected types");
            }

        }
    }
}
