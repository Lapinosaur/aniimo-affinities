using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ZoomTests
    {
        internal static void Run()
        {
            float fitted = WindowZoom.FitScale(1.5f, new SizeF(902, 592), new Size(1024, 728));
            if (902 * fitted > 1008 || 592 * fitted > 712 || fitted > 1.5f)
                throw new Exception("Zoom must fit the working area on small desktops.");
            float savedZoom = DisplaySettings.WindowScale;
            DisplaySettings.WindowScale = 1;
            try
            {
                using (var main = new MainForm())
                using (var matrix = new MatrixDialog(-1))
                {
                    main.Show();
                    matrix.Show();
                    Application.DoEvents();
                    var initial = main.Bounds;
                    var swap = typeof(MainForm).GetMethod("ToggleInlineMatrix", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    for (int i = 0; i < 12; i++)
                    {
                        swap.Invoke(main, null);
                        swap.Invoke(main, null);
                        Application.DoEvents();
                        if (main.Bounds != initial || main.VisualUpdateDepth != 0)
                            throw new Exception("Repeated view toggles must restore bounds without drift or an unfinished layout.");
                    }
                    // Keep the 150% case within small CI desktops so native screen
                    // constraints do not alter the geometry under test.
                    main.MinimumSize = matrix.MinimumSize = Size.Empty;
                    main.ClientSize = matrix.ClientSize = new Size(400, 300);
                    var mainSize = main.ClientSize;
                    var matrixSize = matrix.ClientSize;
                    var work = Screen.FromControl(main).WorkingArea;
                    main.Location = new Point(work.Right - 20 - main.Width, work.Top + 20);
                    int right = main.Right, top = main.Top;
                    foreach (float zoom in new[] { .5f, 1.5f, 1f, .5f, 1f })
                    {
                        DisplaySettings.ApplyDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, zoom);
                        Application.DoEvents();
                        if (Math.Abs(main.ClientSize.Width - mainSize.Width * zoom) > 2 || Math.Abs(matrix.ClientSize.Height - matrixSize.Height * zoom) > 2)
                            throw new Exception("Window scale geometry or rollback failed");
                        if (main.Right != right || main.Top != top || !work.Contains(main.Bounds))
                            throw new Exception("Zoom must preserve the top-right corner and stay on screen");
                        using (var bitmap = new Bitmap(main.Width, main.Height))
                            main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, main.Size));
                    }
                    foreach (Keys key in new[] { Keys.Control | Keys.Up, Keys.Control | Keys.Up, Keys.Control | Keys.Down, Keys.Control | Keys.Down, Keys.Control | Keys.Down, Keys.Control | Keys.Down })
                    {
                        WindowZoom.HandleShortcut(key, IntPtr.Zero);
                        Application.DoEvents();
                        if (main.Right != right || main.Top != top || !work.Contains(main.Bounds))
                            throw new Exception("Ctrl+Up/Down must preserve the top-right anchor");
                    }
                    main.Activate();
                    Application.DoEvents();
                    var wheel = new WindowZoomWheel();
                    float beforeWheel = DisplaySettings.WindowScale;
                    var wheelMessage = Message.Create(main.Handle, 0x020A, new IntPtr((120 << 16) | 8), IntPtr.Zero);
                    wheel.HandleWheel(main, 120);
                    if (DisplaySettings.WindowScale != WindowZoom.Step(beforeWheel, 1))
                        throw new Exception("Ctrl+wheel must immediately zoom one step");
                    if (main.Right != right || main.Top != top || !work.Contains(main.Bounds))
                        throw new Exception("Ctrl+wheel must preserve the top-right anchor");
                    wheelMessage = Message.Create(main.Handle, 0x020A, new IntPtr(120 << 16), IntPtr.Zero);
                    if (wheel.PreFilterMessage(ref wheelMessage))
                        throw new Exception("Plain wheel must remain available for scrolling");
                    main.Location = new Point(work.Left, work.Bottom - main.Height);
                    WindowZoom.HandleShortcut(Keys.Control | Keys.Up, IntPtr.Zero);
                    if (!work.Contains(main.Bounds))
                        throw new Exception("Zoom near screen edges must keep the entire window visible");
                }
            }
            finally { DisplaySettings.WindowScale = savedZoom; }

        }
    }
}
