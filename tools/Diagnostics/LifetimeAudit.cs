using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class LifetimeAudit
    {
        [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process, uint flag);
        private static void Pump(int milliseconds = 50)
        {
            var clock = Stopwatch.StartNew();
            do
            {
                Application.DoEvents();
                Thread.Sleep(1);
            } while (clock.ElapsedMilliseconds < milliseconds);
        }
        private static void Collect()
        {
            Pump();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Pump();
        }
        private static void Track(Form form, List<WeakReference> tracked, bool paint = true)
        {
            tracked.Add(new WeakReference(form));
            if (Environment.GetEnvironmentVariable("AFFINISAUR_AUDIT_OFFSCREEN") == "1")
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-32000, -32000);
            }
            form.Show();
            Pump();
            if (paint)
                using (var bitmap = new Bitmap(form.Width, form.Height))
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Cycle(string scenario, List<WeakReference> tracked)
        {
            var plan = TeamTests.Sample();
            switch (scenario)
            {
                case "plain":
                    using (var form = new Form())
                        Track(form, tracked, false);
                    break;
                case "plain-painted":
                    using (var form = new Form())
                        Track(form, tracked);
                    break;
                case "plain-closed":
                    using (var form = new Form())
                    {
                        Track(form, tracked);
                        form.Close();
                    }
                    break;
                case "main":
                    using (var form = new MainForm())
                        Track(form, tracked);
                    break;
                case "settings":
                    using (var form = new SettingsDialog(KeyBindings.Defaults()))
                        Track(form, tracked);
                    break;
                case "display":
                    using (var form = new DisplayDialog())
                        Track(form, tracked);
                    break;
                case "radar":
                    using (var form = new RadarDialog(0, 1, KeyBindings.Defaults()))
                    {
                        Track(form, tracked);
                        WindowZoom.SetScale(form, .5f);
                        WindowZoom.SetScale(form, 1);
                    }
                    break;
                case "matrix":
                    using (var form = new MatrixDialog(-1))
                        Track(form, tracked);
                    break;
                case "team-editor":
                    using (var form = new TeamEditor(plan, () => { }))
                        Track(form, tracked);
                    break;
                case "suggestions":
                    using (var form = new SuggestionsDialog(plan, 3, (slot, member) => false))
                    {
                        Track(form, tracked);
                        form.SelectSlot(4);
                        form.SelectSlot(5);
                    }
                    break;
                case "support":
                    using (var form = new SupportWindow())
                    {
                        Track(form, tracked);
                        for (int i = 0; i < 10; i++)
                            SupportWindow.Open(form);
                    }
                    break;
                case "onboarding":
                    using (var form = new OnboardingWindow())
                    {
                        Track(form, tracked);
                        for (int i = 0; i < 3; i++)
                            form.Navigate(1);
                    }
                    break;
                case "update":
                    using (var form = new UpdateWindow(new ReleaseUpdate { Version = "Demo", Changes = new[] { new ReleaseChange { Version = "Demo", Notes = string.Join("\n", Enumerable.Repeat("- **Example** release notes", 50)) } } }))
                        Track(form, tracked);
                    break;
                case "bubble":
                    using (var host = new Form())
                    using (var bubble = new TipBubble())
                    {
                        Track(host, tracked);
                        tracked.Add(new WeakReference(bubble));
                        bubble.ScrollGraphic = true;
                        bubble.SetPreviewAction(() => { });
                        bubble.SetApplyAction(() => { });
                        bubble.Present(host, "Example", null, null, (g, r) => g.FillRectangle(Brushes.Red, r), new Size(300, 1800));
                        Pump();
                        bubble.Hide();
                    }
                    break;
                case "tutorial":
                    using (var source = new MainForm())
                    using (var tour = new TutorialOverlay(source))
                    {
                        tracked.Add(new WeakReference(source));
                        Track(tour, tracked);
                        for (int i = 1; i < tour.StepCount; i++)
                            tour.NavigateStep(1);
                    }
                    break;
                case "suggestion-scenes":
                    foreach (string view in new[] { "suggest-coverage", "suggest-counter", "suggest-radar", "suggest-apply" })
                        using (var scene = new SuggestionTutorialScene(view))
                        using (var frame = scene.Capture())
                        {
                        }
                    break;
                case "interactive-glass":
                    using (var form = new MainForm())
                    {
                        Track(form, tracked);
                        for (int i = 0; i < 3; i++)
                        {
                            form.PreviewWindowGlass(form, true);
                            Pump();
                            form.Location += new Size(2, 2);
                            Pump();
                            form.Hide();
                            form.Show();
                            Pump();
                            form.EndWindowGlassPreview(form);
                            Pump();
                        }
                    }
                    break;
                case "locked-glass":
                    using (var form = new MainForm())
                    {
                        Track(form, tracked);
                        var toggle = typeof(PortableWindow).GetMethod("SetOverlayLocked", BindingFlags.Instance | BindingFlags.NonPublic);
                        for (int i = 0; i < 3; i++)
                        {
                            toggle.Invoke(form, new object[] { true });
                            Pump();
                            form.Location += new Size(2, 2);
                            Pump();
                            toggle.Invoke(form, new object[] { false });
                            Pump();
                        }
                    }
                    break;
            }
        }
        private static long[] Snapshot()
        {
            Collect();
            using (var process = Process.GetCurrentProcess())
                return new[] { GC.GetTotalMemory(false), process.PrivateMemorySize64, process.HandleCount, GetGuiResources(process.Handle, 0), GetGuiResources(process.Handle, 1), Application.OpenForms.Count };
        }
        internal static void Run(string path)
        {
            var report = new StringBuilder("scenario,batch,managed_bytes,private_bytes,handles,gdi,user,open_forms,retained_forms\n");
            foreach (string scenario in (Environment.GetEnvironmentVariable("AFFINISAUR_AUDIT_CASES") ?? "plain,plain-painted,main,settings,display,radar,matrix,team-editor,suggestions,support,onboarding,update,bubble,tutorial,suggestion-scenes,interactive-glass,locked-glass").Split(','))
            {
                var tracked = new List<WeakReference>();
                for (int i = 0; i < 2; i++)
                    Cycle(scenario, tracked);
                tracked.Clear();
                for (int batch = 0; batch < 3; batch++)
                {
                    if (batch > 0)
                        for (int i = 0; i < 4; i++)
                            Cycle(scenario, tracked);
                    long[] values = Snapshot();
                    report.AppendLine(scenario + "," + batch + "," + string.Join(",", values.Select(value => value.ToString()).ToArray()) + "," + tracked.Count(reference => reference.IsAlive));
                    File.WriteAllText(path, report.ToString());
                }
                Console.WriteLine("AUDITED " + scenario + " (2 warmup + 8 cycles)");
                Pump(3000);
                long[] settled = Snapshot();
                report.AppendLine(scenario + ",settled," + string.Join(",", settled.Select(value => value.ToString()).ToArray()) + "," + tracked.Count(reference => reference.IsAlive));
                File.WriteAllText(path, report.ToString());
            }
            if (Environment.GetEnvironmentVariable("AFFINISAUR_AUDIT_HOLD") == "1")
            {
                Console.WriteLine("HEAP PID " + Process.GetCurrentProcess().Id);
                Pump(60000);
            }
        }
    }
}
