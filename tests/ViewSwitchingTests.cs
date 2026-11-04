using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ViewSwitchingTests
    {
        private static Button FindMinimize(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                var button = control as Button;
                if (button != null && button.AccessibleName == "Minimize")
                    return button;
                var nested = FindMinimize(control);
                if (nested != null)
                    return nested;
            }
            return null;
        }
        internal static void Run()
        {
            using (var main = new MainForm())
            using (var guard = new Timer { Interval = 2000 })
            {
                main.Show();
                guard.Tick += (s, e) => { foreach (var child in main.OwnedForms) child.Close(); };
                guard.Start();
                var open = typeof(MainForm).GetMethod("OpenView", BindingFlags.NonPublic | BindingFlags.Instance);
                open.Invoke(main, new object[] { "radar" });
                open.Invoke(main, new object[] { "matrix" });
                open.Invoke(main, new object[] { "radar" });
                guard.Stop();
                var children = Application.OpenForms.Cast<Form>().Where(child => child is RadarDialog || child is MatrixDialog).ToArray();
                if (children.Length != 2 || children.Any(child => child.Modal || !child.Visible) || !main.Enabled)
                    throw new Exception("Content windows must coexist without blocking the main window or duplicating the same view.");
                main.WindowState = FormWindowState.Minimized;
                Application.DoEvents();
                if (ContentWindowPreferences.KeepVisible && children.Any(child => child.Owner != null || !child.Visible))
                    throw new Exception("Independent content windows must stay visible when the main window is minimized.");
                main.WindowState = FormWindowState.Normal;
                float global = DisplaySettings.WindowScale;
                var radar = (PortableWindow)children.Single(child => child is RadarDialog);
                var matrix = (PortableWindow)children.Single(child => child is MatrixDialog);
                foreach (var child in children)
                {
                    var minimize = FindMinimize(child);
                    if (minimize == null || !minimize.Visible || !minimize.Enabled)
                        throw new Exception("Each content window must expose its own minimize button.");
                    minimize.PerformClick();
                    Application.DoEvents();
                    if (child.WindowState != FormWindowState.Minimized || main.WindowState != FormWindowState.Normal || children.Any(other => other != child && other.WindowState == FormWindowState.Minimized))
                        throw new Exception("A content minimize button must affect only its own window.");
                    child.WindowState = FormWindowState.Normal;
                }
                float matrixScale = matrix.AppliedWindowScale;
                WindowZoom.SetScale(radar, .5f);
                var work = Screen.FromControl(radar).WorkingArea;
                radar.Location = new Point(work.Right - radar.Width - 12, work.Top + 20);
                int radarRight = radar.Right;
                var modes = radar.Controls.OfType<TypeSelector>().SelectMany(row => row.Controls.OfType<IconButton>()).Where(button => button.AccessibleName == "Combined radars" || button.AccessibleName == "Separate radars").ToArray();
                int compactWidth = radar.Width;
                modes.Single(button => button.AccessibleName == "Separate radars").PerformClick();
                if (radar.Width <= compactWidth || radar.Right != radarRight)
                    throw new Exception("Separate radar must expand from the same top-right corner.");
                modes.Single(button => button.AccessibleName == "Combined radars").PerformClick();
                if (radar.Width != compactWidth || radar.Right != radarRight)
                    throw new Exception("Combined radar must hug its content without anchor drift.");
                if (DisplaySettings.WindowScale != global || matrix.AppliedWindowScale != matrixScale || Math.Abs(radar.AppliedWindowScale - .5f) > .01f)
                    throw new Exception("Child zoom must be temporary and isolated.");
                radar.Activate();
                Application.DoEvents();
                var hoverPoint = radar.PointToScreen(new Point(20, 20));
                var wheelMessage = Message.Create(main.Handle, 0x020A, new IntPtr((120 << 16) | 8), new IntPtr((hoverPoint.Y << 16) | (hoverPoint.X & 0xffff)));
                // Native hit testing depends on foreground ownership of the test desktop.
                // Supply the hovered control while keeping message routing and zoom real.
                if (!new WindowZoomWheel(point => radar).PreFilterMessage(ref wheelMessage) || Math.Abs(radar.AppliedWindowScale - .75f) > .01f || DisplaySettings.WindowScale != global)
                    throw new Exception("Wheel input must target the hovered child even when delivered to the parent HWND.");
                WindowZoom.SetScale(radar, .5f);
                WindowZoom.SetScale(main, .75f);
                if (Math.Abs(radar.AppliedWindowScale - .5f) > .01f || matrix.AppliedWindowScale != matrixScale)
                    throw new Exception("Parent shortcuts must not resize existing content windows.");
                DisplaySettings.ApplyDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, .75f);
                if (Math.Abs(radar.AppliedWindowScale - .75f) > .01f || Math.Abs(matrix.AppliedWindowScale - .75f) > .01f)
                    throw new Exception("Settings must still synchronize all content window scales.");
                DisplaySettings.ApplyDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, global);
                bool keepVisible = ContentWindowPreferences.KeepVisible;
                try
                {
                    ContentWindowPreferences.Save(false);
                    if (children.Any(child => child.Owner != main))
                        throw new Exception("Minimize-together preference must restore ownership.");
                    ContentWindowPreferences.Save(true);
                    if (children.Any(child => child.Owner != null))
                        throw new Exception("Independent preference must detach native ownership.");
                }
                finally { ContentWindowPreferences.Save(keepVisible); }
                main.Close();
                if (children.Any(child => !child.IsDisposed))
                    throw new Exception("Closing the main window must dispose its modeless content windows.");
            }
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
