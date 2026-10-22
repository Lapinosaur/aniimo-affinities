using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class ResourceProbe
    {
        [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process, uint flag);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OpenClose(List<WeakReference> windows)
        {
            using (var main = new MainForm())
            using (var matrix = new MatrixDialog(-1))
            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            {
                main.Show();
                matrix.Show();
                settings.Show();
                Application.DoEvents();
                windows.Add(new WeakReference(main));
                windows.Add(new WeakReference(matrix));
                windows.Add(new WeakReference(settings));
            }
            Application.DoEvents();
        }
        private static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Application.DoEvents();
        }
        internal static void Run(string report)
        {
            AppDomain.MonitoringIsEnabled = true;
            var windows = new List<WeakReference>();
            for (int i = 0; i < 3; i++)
                OpenClose(windows);
            windows.Clear();
            Collect();
            using (var process = Process.GetCurrentProcess())
            {
                uint beforeGdi = GetGuiResources(process.Handle, 0), beforeUser = GetGuiResources(process.Handle, 1);
                for (int i = 0; i < 20; i++)
                    OpenClose(windows);
                Collect();
                int retained = windows.FindAll(reference => reference.IsAlive).Count;
                uint afterGdi = GetGuiResources(process.Handle, 0), afterUser = GetGuiResources(process.Handle, 1);
                long allocated;
                double elapsed;
                using (var main = new MainForm())
                using (var matrix = new MatrixDialog(-1))
                using (var settings = new SettingsDialog(KeyBindings.Defaults()))
                {
                    main.Show();
                    matrix.Show();
                    settings.Show();
                    Application.DoEvents();
                    long start = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
                    var clock = Stopwatch.StartNew();
                    for (int i = 0; i < 200; i++)
                        DisplaySettings.ApplyDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, DisplaySettings.WindowScale);
                    elapsed = clock.Elapsed.TotalMilliseconds;
                    allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - start;
                }
                File.WriteAllLines(report, new[]{
                    "Open/close cycles: 20 (60 forms)","Forms retained after collection: "+retained,
                    "GDI resources before/after: "+beforeGdi+" / "+afterGdi,
                    "USER resources before/after: "+beforeUser+" / "+afterUser,
                    "200 unchanged display updates: "+elapsed.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" ms",
                    "Managed bytes allocated during unchanged updates: "+allocated
                });
            }
        }
    }
}
