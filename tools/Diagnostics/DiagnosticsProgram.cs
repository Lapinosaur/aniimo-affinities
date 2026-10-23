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
                return Run(args);
        }
        // Keep preference-dependent static initializers out of Main's JIT compilation.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
            try
            {
                ApplicationBootstrap.Initialize();
                if (args.Length == 1 && args[0] == "--test-zoom")
                {
                    ZoomTests.Run();
                    return 0;
                }
                if (args.Length == 1 && args[0] == "--test-bubble-clicks")
                {
                    BubbleClickTests.Run();
                    return 0;
                }
                if (args.Length == 1 && args[0] == "--test-transparency")
                {
                    TransparencyTests.Run();
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--profile-interactions")
                {
                    InteractionProbe.Run(args[1]);
                    return 0;
                }
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
                if (args.Length == 2 && args[0].StartsWith("--preview-busy-"))
                {
                    BusyPreview.Save(args[0].Substring("--preview-busy-".Length), args[1]);
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--preview-components")
                {
                    ComponentPreview.Save(args[1]);
                    return 0;
                }
                if (args.Length == 2 && (args[0] == "--preview-glass" || args[0] == "--preview-glass-radar" || args[0] == "--preview-glass-matrix"))
                {
                    GlassPreview.Save(args[1], args[0] == "--preview-glass-radar" ? "radar" : args[0] == "--preview-glass-matrix" ? "matrix" : "main");
                    return 0;
                }
                if (args.Length == 2 && args[0].StartsWith("--preview-team"))
                {
                    TeamPreview.Save(args[0].Substring("--preview-".Length), args[1]);
                    return 0;
                }
                if (args.Length == 2 && new[] { "--preview-tutorial", "--preview-tutorial-shortcuts", "--preview-tutorial-radar-select", "--preview-tutorial-radar-hover", "--preview-tutorial-suggestions-coverage", "--preview-tutorial-suggestions-counter", "--preview-tutorial-suggestions-radar", "--preview-tutorial-suggestions-apply", "--preview-tutorial-pin", "--preview-tutorial-hold", "--preview-tutorial-hold-lock", "--preview-tutorial-locked", "--preview-tutorial-news", "--preview-tutorial-selector", "--preview-tutorial-display", "--preview-tutorial-final" }.Contains(args[0]))
                {
                    bool newsPreview = args[0] == "--preview-tutorial-news";
                    if (newsPreview)
                        new TutorialProgress { Sections = TutorialProgress.News }.Save();
                    using (var source = new MainForm())
                    using (var tour = new TutorialOverlay(source, newsPreview))
                    {
                        source.Show();
                        Application.DoEvents();
                        tour.Show(source);
                        Application.DoEvents();
                        int target = args[0] == "--preview-tutorial-shortcuts" ? 14 : args[0] == "--preview-tutorial-radar-select" ? 8 : args[0] == "--preview-tutorial-radar-hover" ? 9 : args[0] == "--preview-tutorial-suggestions-coverage" ? 27 : args[0] == "--preview-tutorial-suggestions-counter" ? 28 : args[0] == "--preview-tutorial-suggestions-radar" ? 29 : args[0] == "--preview-tutorial-suggestions-apply" ? 30 : args[0] == "--preview-tutorial-pin" ? 10 : (args[0] == "--preview-tutorial-hold" || args[0] == "--preview-tutorial-hold-lock") ? 12 : args[0] == "--preview-tutorial-locked" ? 13 : args[0] == "--preview-tutorial-news" ? 2 : args[0] == "--preview-tutorial-final" ? tour.StepCount - 1 : args[0] == "--preview-tutorial-display" ? 16 : args[0] == "--preview-tutorial-selector" ? 1 : 0;
                        while (tour.StepIndex < target)
                            tour.NavigateStep(1);
                        if (args[0].StartsWith("--preview-tutorial-radar-", StringComparison.Ordinal))
                        {
                            var clock = System.Diagnostics.Stopwatch.StartNew();
                            while (clock.ElapsedMilliseconds < 5200)
                            {
                                Application.DoEvents();
                                System.Threading.Thread.Sleep(15);
                            }
                        }
                        if (args[0].StartsWith("--preview-tutorial-suggestions", StringComparison.Ordinal))
                        {
                            var animate = System.Diagnostics.Stopwatch.StartNew();
                            int wait = args[0].EndsWith("apply", StringComparison.Ordinal) ? 5200 : 2800;
                            while (animate.ElapsedMilliseconds < wait)
                            {
                                Application.DoEvents();
                                System.Threading.Thread.Sleep(5);
                            }
                        }
                        if (args[0].StartsWith("--preview-tutorial-hold", StringComparison.Ordinal))
                        {
                            var animation = System.Diagnostics.Stopwatch.StartNew();
                            int wait = args[0] == "--preview-tutorial-hold-lock" ? 2200 : 1200;
                            while (animation.ElapsedMilliseconds < wait)
                            {
                                Application.DoEvents();
                                System.Threading.Thread.Sleep(5);
                            }
                        }
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
                    using (PortableWindow form = args[0] == "--preview-storage" ? (PortableWindow)new StorageWindow() : new UpdateWindow(new ReleaseUpdate
                    {
                        Version = "v0.7.0",
                        HistoryComplete = true,
                        Changes = new[] {
                        new ReleaseChange { Version = "v0.7.0", Notes = "## Example release notes (preview fixture)\n- Individual tutorials and saved progress.\n- Shared keyboard focus and disabled states." },
                        new ReleaseChange { Version = "v0.6.0", Notes = "## Example intermediate release\n- Improved panel transitions.\n- Clearer shortcut conflict feedback." }
                    }
                    }))
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
                    using (var form = new UnlockWindow(() => { }, () => { }, false, () => Point.Empty, point => { }, () => { }, () => "team-Right"))
                    {
                        form.Show();
                        Application.DoEvents();
                        using (var image = UnlockAppearance.Compose(form))
                        {
                            image.Save(args[1]);
                        }
                    }
                    return 0;
                }
                if (args.Length == 2 && (args[0] == "--preview-tooltip" || args[0] == "--preview-shortcut-tooltip" || args[0] == "--preview-about-tooltip"))
                {
                    bool about = args[0] == "--preview-about-tooltip";
                    using (var owner = about ? (Form)new SettingsDialog(KeyBindings.Defaults()) : new Form())
                    using (var bubble = new TipBubble())
                    {
                        var anchor = about ? owner.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "About & Credits") : (Control)new Button { Text = "Grass", Size = new Size(80, 30) };
                        if (!about)
                            owner.Controls.Add(anchor);
                        owner.Show();
                        Application.DoEvents();
                        bubble.Present(anchor, about ? anchor.AccessibleDescription : args[0] == "--preview-shortcut-tooltip" ? "Hold Ctrl and press a number to select an element. Ctrl+0 clears the row. Esc closes this window." : "Hold and drag to move overlay");
                        Application.DoEvents();
                        using (var image = new Bitmap(bubble.Width, bubble.Height))
                        {
                            bubble.DrawToBitmap(image, new Rectangle(Point.Empty, bubble.Size));
                            image.Save(args[1]);
                        }
                    }
                    return 0;
                }
                if (args.Length == 1 && args[0] == "--test-groups")
                {
                    Console.WriteLine(string.Join(",", RegressionTests.Groups()));
                    return 0;
                }
                if ((args.Length == 2 || args.Length == 3) && args[0] == "--self-test")
                {
                    try
                    {
                        File.WriteAllText(args[1], RegressionTests.Run(args.Length == 3 ? args[2] : "all"));
                    }
                    catch (Exception error)
                    {
                        File.WriteAllText(args[1], error.ToString());
                        throw;
                    }
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
                if (args.Length == 2 && (args[0] == "--preview-display-settings" || args[0] == "--preview-display-theme-hover" || args[0] == "--preview-guide-settings" || args[0] == "--preview-about-settings"))
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
                        if (args[0] == "--preview-display-theme-hover")
                        {
                            var display = form.Controls.OfType<DisplayDialog>().Single();
                            var choice = display.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<ThemePreview>()).Single(preview => preview.Mode == 2);
                            typeof(Control).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(choice, new object[] { EventArgs.Empty });
                            Application.DoEvents();
                        }
                        using (var image = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                            image.Save(args[1]);
                        }
                    }
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--preview-settings-error")
                {
                    using (var form = new ShortcutConflictWindow("2", "Dark", "Earth", "1"))
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
                if (args.Length == 2 && (args[0] == "--preview-settings" || args[0] == "--preview-settings-test" || args[0] == "--preview-settings-error" || args[0] == "--preview-window-settings"))
                {
                    using (var form = new SettingsDialog(KeyBindings.Defaults()))
                    {
                        form.Show();
                        Application.DoEvents();
                        if (args[0] == "--preview-window-settings")
                            form.TutorialSection("Window & overlay");
                        if (args[0] == "--preview-settings-test")
                            form.TestShortcut(Keys.D2, true);
                        using (var image = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                            image.Save(args[1]);
                        }
                    }
                    return 0;
                }
                if (args.Length == 2 && (args[0] == "--preview" || args[0] == "--preview-child" || args[0] == "--preview-inline-matrix" || args[0] == "--preview-inline-radar" || args[0] == "--preview-small" || args[0] == "--preview-empty" || args[0] == "--preview-overlay" || args[0] == "--preview-locked-overlay"))
                {
                    using (var form = new MainForm(false, args[0] == "--preview-child"))
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
                        if (args[0] == "--preview-locked-overlay")
                        {
                            typeof(PortableWindow).GetMethod("SetOverlayLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(form, new object[] { true });
                            Application.DoEvents();
                        }
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
