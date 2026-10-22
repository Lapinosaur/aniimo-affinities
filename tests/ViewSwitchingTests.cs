using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ViewSwitchingTests
    {
        internal static void Run()
        {
            using (var main = new MainForm())
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var toggle = typeof(MainForm).GetMethod("ToggleInlineRadar", flags);
                var state = typeof(MainForm).GetField("inlineRadar", flags);
                toggle.Invoke(main, null);
                if (!(bool)state.GetValue(main))
                    throw new Exception("Inline radar did not activate");
                var matrixToggle = typeof(MainForm).GetMethod("ToggleInlineMatrix", flags);
                matrixToggle.Invoke(main, null);
                if ((string)typeof(MainForm).GetField("currentView", flags).GetValue(main) != "matrix" || (string)typeof(MainForm).GetField("matrixView", flags).GetValue(main) != "radar")
                    throw new Exception("Matrix must swap with active radar");
                matrixToggle.Invoke(main, null);
                toggle.Invoke(main, null);
                if ((bool)state.GetValue(main))
                    throw new Exception("Affinity diagrams were not restored");
            }
        }
    }
}
