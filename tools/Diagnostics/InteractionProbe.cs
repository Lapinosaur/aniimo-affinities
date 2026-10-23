using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class InteractionProbe
    {
        internal static void Run(string path)
        {
            AppDomain.MonitoringIsEnabled = true;
            var output = new StringBuilder();
            TransparencyPreferences.Save(true, true, true);
            using (var main = new MainForm())
            {
                main.Show();
                Application.DoEvents();
                var glass = (InteractiveGlass)typeof(PortableWindow).GetField("interactiveGlass", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(main);
                output.AppendLine("Native glass active: " + (glass != null && glass.Active));
                Point origin = main.Location;
                Action move = () => { main.Location = new Point(origin.X + 1, origin.Y + 1); main.Location = origin; Application.DoEvents(); };
                Measure(output, "Glass movement pair", move, 120);
            }
            using (var host = new Form { Size = new Size(800, 600) })
            using (var report = new ThreatReportPanel { Dock = DockStyle.Fill })
            {
                host.Controls.Add(report);
                host.Show();
                var team = new TeamPlan();
                team.Allies[0].Main = 0;
                team.Allies[0].Sub = 6;
                typeof(ThreatReportPanel).GetField("mode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(report, 2);
                report.UpdateReport(team, false);
                Application.DoEvents();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(ThreatReportPanel).GetMethod("ShowCellTip", flags).Invoke(report, new object[] { 4, 0 });
                var tips = (ApplicationTips)typeof(ThreatReportPanel).GetField("tips", flags).GetValue(report);
                var painters = (System.Collections.Generic.Dictionary<Control, Action<Graphics, RectangleF>>)typeof(ApplicationTips).GetField("graphics", flags).GetValue(tips);
                var sizes = (System.Collections.Generic.Dictionary<Control, Size>)typeof(ApplicationTips).GetField("graphicSizes", flags).GetValue(tips);
                var table = painters.Keys.OfType<DataGridView>().Single();
                output.AppendLine("Matchup rows: " + sizes[table].Height / 82);
                using (var image = new Bitmap(250, 480))
                using (var graphics = Graphics.FromImage(image))
                {
                    int step = 0;
                    Action paint = () =>
                    {
                        graphics.ResetTransform();
                        graphics.SetClip(new Rectangle(0, 0, 250, 480));
                        graphics.TranslateTransform(0, -(step++ % 5) * 82);
                        painters[table](graphics, new RectangleF(Point.Empty, sizes[table]));
                    };
                    Measure(output, "Long bubble clipped paint", paint, 120);
                }
            }
            File.WriteAllText(path, output.ToString());
        }
        private static void Measure(StringBuilder output, string name, Action action, int count)
        {
            for (int i = 0; i < 10; i++)
                action();
            var timings = new double[count];
            long before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            var timer = new Stopwatch();
            for (int i = 0; i < count; i++)
            {
                timer.Restart();
                action();
                timer.Stop();
                timings[i] = timer.Elapsed.TotalMilliseconds;
            }
            long bytes = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before;
            Array.Sort(timings);
            output.AppendLine(name + ": median=" + timings[count / 2].ToString("F3") + " ms; p95=" + timings[(int)(count * .95)].ToString("F3") + " ms; allocated=" + bytes + " bytes / " + count);
        }
    }
}

