using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class DiagnosticsProgram
    {
        [STAThread]
        private static int Main(string[] args)
        {
            using (var environment = new DiagnosticEnvironment())
                try
                {
                    ApplicationBootstrap.Initialize();
                    if (args.Length == 2 && args[0] == "--profile-resources")
                    {
                        ResourceProbe.Run(args[1]);
                        return 0;
                    }
                    if (args.Length == 2 && args[0].StartsWith("--preview-half-"))
                    {
                        DisplaySettings.WindowScale = .5f;
                        args[0] = "--preview-" + args[0].Substring("--preview-half-".Length);
                    }
                    if (args.Length == 2 && args[0].StartsWith("--preview-large-"))
                    {
                        DisplaySettings.WindowScale = 1.5f;
                        args[0] = "--preview-" + args[0].Substring("--preview-large-".Length);
                    }
                    if (args.Length == 2 && args[0].StartsWith("--preview-light-"))
                    {
                        Theme.Appearance = 2;
                        Theme.ApplyAppearance();
                        args[0] = "--preview-" + args[0].Substring("--preview-light-".Length);
                    }
                    if (args.Length == 2 && args[0].StartsWith("--preview-dark-"))
                    {
                        Theme.Appearance = 0;
                        Theme.ApplyAppearance();
                        args[0] = "--preview-" + args[0].Substring("--preview-dark-".Length);
                    }
                    if (args.Length == 2 && new[] { "--preview-tutorial", "--preview-tutorial-selector", "--preview-tutorial-display", "--preview-tutorial-final" }.Contains(args[0]))
                    {
                        using (var source = new MainForm())
                        using (var tour = new TutorialOverlay(source))
                        {
                            source.Show();
                            Application.DoEvents();
                            tour.Show(source);
                            Application.DoEvents();
                            int target = args[0] == "--preview-tutorial-final" ? tour.StepCount - 1 : args[0] == "--preview-tutorial-display" ? 14 : args[0] == "--preview-tutorial-selector" ? 1 : 0;
                            while (tour.StepIndex < target)
                                tour.NavigateStep(1);
                            Application.DoEvents();
                            using (var image = new Bitmap(tour.Width, tour.Height))
                            {
                                tour.DrawToBitmap(image, new Rectangle(Point.Empty, tour.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && (args[0] == "--preview-storage" || args[0] == "--preview-update"))
                    {
                        using (PortableWindow form = args[0] == "--preview-storage" ? (PortableWindow)new StorageWindow() : new UpdateWindow(new ReleaseUpdate { Version = "v0.5.0" }))
                        {
                            form.Show();
                            Application.DoEvents();
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && args[0] == "--preview-lock-controls")
                    {
                        using (var form = new UnlockWindow(() => { }, () => { }, false, () => Point.Empty, point => { }))
                        {
                            form.Show();
                            Application.DoEvents();
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && args[0] == "--preview-tooltip")
                    {
                        using (var owner = new Form())
                        using (var bubble = new TipBubble())
                        {
                            var anchor = new Button { Text = "Grass", Size = new Size(80, 30) };
                            owner.Controls.Add(anchor);
                            owner.Show();
                            Application.DoEvents();
                            bubble.Present(anchor, "Hold and drag to move overlay");
                            Application.DoEvents();
                            using (var image = new Bitmap(bubble.Width, bubble.Height))
                            {
                                bubble.DrawToBitmap(image, new Rectangle(Point.Empty, bubble.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && args[0] == "--self-test")
                    {
                        RegressionTests.Run();
                        File.WriteAllText(args[1], "PASS: 81 selections, 9 SVG icons, selection transitions, no capture modules.");
                        return 0;
                    }
                    if (args.Length == 2 && (args[0] == "--preview-radar" || args[0] == "--preview-radar-separate"))
                    {
                        using (var form = new RadarDialog(Array.IndexOf(Model.Names, "Grass"), Array.IndexOf(Model.Names, "Water"), KeyBindings.Defaults()))
                        {
                            form.Show();
                            Application.DoEvents();
                            if (args[0] == "--preview-radar-separate")
                                ((IconButton)typeof(RadarDialog).GetField("separateButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form)).PerformClick();
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && args[0] == "--preview-display")
                    {
                        using (var form = new DisplayDialog())
                        {
                            form.Show();
                            Application.DoEvents();
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && args[0] == "--preview-matrix")
                    {
                        using (var form = new MatrixDialog(-1))
                        {
                            form.Show();
                            Application.DoEvents();
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && (args[0] == "--preview-display-settings" || args[0] == "--preview-guide-settings" || args[0] == "--preview-about-settings"))
                    {
                        using (var form = new SettingsDialog(KeyBindings.Defaults()))
                        {
                            form.Show();
                            Application.DoEvents();
                            if (args[0] == "--preview-guide-settings" || args[0] == "--preview-about-settings")
                            {
                                form.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == (args[0] == "--preview-about-settings" ? "About & Credits" : "Tutorial")).PerformClick();
                                var settle = System.Diagnostics.Stopwatch.StartNew();
                                while (settle.ElapsedMilliseconds < 230)
                                {
                                    Application.DoEvents();
                                    System.Threading.Thread.Sleep(5);
                                }
                            }
                            else
                            {
                                form.TutorialSection("Display");
                                var navigation = Message.Create(form.Handle, 0x100, (IntPtr)Keys.Tab, IntPtr.Zero);
                                form.PreFilterMessage(ref navigation);
                            }
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && (args[0] == "--preview-settings" || args[0] == "--preview-window-settings"))
                    {
                        using (var form = new SettingsDialog(KeyBindings.Defaults()))
                        {
                            form.Show();
                            Application.DoEvents();
                            if (args[0] == "--preview-window-settings")
                                form.TutorialSection("Window & overlay");
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    if (args.Length == 2 && (args[0] == "--preview" || args[0] == "--preview-inline-matrix" || args[0] == "--preview-inline-radar" || args[0] == "--preview-small" || args[0] == "--preview-empty" || args[0] == "--preview-overlay"))
                    {
                        using (var form = new MainForm())
                        {
                            if (args[0] == "--preview-inline-radar")
                                typeof(MainForm).GetMethod("ToggleInlineRadar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(form, null);
                            if (args[0] == "--preview-inline-matrix")
                                typeof(MainForm).GetMethod("ToggleInlineMatrix", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(form, null);
                            if (args[0] == "--preview-overlay")
                                form.SetPinned(true);
                            if (args[0] == "--preview-small" || args[0] == "--preview-empty")
                                form.Size = form.MinimumSize;
                            if (args[0] == "--preview-empty")
                                ((Selection)typeof(MainForm).GetField("selection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form)).Clear();
                            if (args[0] == "--preview-empty")
                                typeof(MainForm).GetMethod("RefreshSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(form, null);
                            form.Show();
                            Application.DoEvents();
                            using (var image = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                                image.Save(args[1]);
                            }
                        }
                        return 0;
                    }
                    throw new ArgumentException("Use --self-test <report> or --preview[-variant] <image>.");
                }
                catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
    }
}
