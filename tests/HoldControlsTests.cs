using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class HoldControlsTests
    {
        internal static void Run()
        {
            float[][] expectedZoomTargets = { new[] { .75f, 1f }, new[] { .5f, 1f }, new[] { .5f, 1.5f }, new[] { 1f, 1.5f }, new[] { 1f, 1.25f } };
            for (int i = 0; i < 5; i++)
                if (!WindowZoom.Neighbors(.5f + i * .25f).SequenceEqual(expectedZoomTargets[i]))
                    throw new Exception("Locked scale targets are incorrect");
            using (var holder = new Form())
            using (var button = new HoldSizeButton { Target = .5f })
            {
                holder.Controls.Add(button);
                holder.Show();
                Application.DoEvents();
                int commits = 0;
                button.Commit += value => commits++;
                button.PerformClick();
                if (commits != 0)
                    throw new Exception("Short click must not resize overlay");
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var down = typeof(HoldSizeButton).GetMethod("OnMouseDown", flags);
                down.Invoke(button, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0) });
                button.CancelHold();
                if (commits != 0)
                    throw new Exception("Cancelled hold must not resize");
                down.Invoke(button, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0) });
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < 1100)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
                if (commits != 1)
                    throw new Exception("Completed hold must resize exactly once");
            }

        }
    }
}
