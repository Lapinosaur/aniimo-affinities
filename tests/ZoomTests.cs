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
                    // Keep the 150% case within small CI desktops so native screen
                    // constraints do not alter the geometry under test.
                    main.MinimumSize = matrix.MinimumSize = Size.Empty;
                    main.ClientSize = matrix.ClientSize = new Size(400, 300);
                    var mainSize = main.ClientSize;
                    var matrixSize = matrix.ClientSize;
                    foreach (float zoom in new[] { .5f, 1.5f, 1f, .5f, 1f })
                    {
                        DisplaySettings.ApplyDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, zoom);
                        Application.DoEvents();
                        if (Math.Abs(main.ClientSize.Width - mainSize.Width * zoom) > 2 || Math.Abs(matrix.ClientSize.Height - matrixSize.Height * zoom) > 2)
                            throw new Exception("Window scale geometry or rollback failed");
                        using (var bitmap = new Bitmap(main.Width, main.Height))
                            main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, main.Size));
                    }
                }
            }
            finally { DisplaySettings.WindowScale = savedZoom; }

        }
    }
}
