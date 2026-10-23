using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class TransparencyTests
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
        internal static void Run()
        {
            string path = PreferenceFiles.FilePath("transparency-test.txt");
            bool oldLock = TransparencyPreferences.Locked, oldChildren = TransparencyPreferences.Children, oldMain = TransparencyPreferences.Main;
            try
            {
                TransparencyPreferences.Load(path);
                if (!TransparencyPreferences.Locked || TransparencyPreferences.Children || TransparencyPreferences.Main)
                    throw new Exception("Fresh defaults must retain glass only for lock.");
                using (var main = new MainForm())
                using (var child = new TransparencyDialog(() => { }))
                {
                    main.Show();
                    child.Show(main);
                    Pump();
                    TransparencyPreferences.Save(true, true, false, path);
                    Pump();
                    if (main.Opacity != 1)
                        throw new Exception("Child preference must not affect main.");
                    VerifyInteractive(child);
                    child.TopMost = true;
                    child.Activate();
                    Pump();
                    using (var entry = new TextBox { Bounds = new Rectangle(24, 322, 120, 24) })
                    {
                        child.Controls.Add(entry);
                        entry.Focus();
                        NativeMethods.SendMessage(entry.Handle, 0x102, new IntPtr('A'), new IntPtr(1));
                        Pump();
                        if (!entry.Focused || entry.Text != "A")
                            throw new Exception("Glass must preserve native keyboard input and text editing.");
                    }
                    var apply = child.Controls.OfType<SettingsButton>().Single(button => button.Text == "Apply");
                    var point = apply.PointToScreen(new Point(apply.Width / 2, apply.Height / 2));
                    if (GetAncestor(WindowFromPoint(point), 2) != child.Handle)
                        throw new Exception("Glass must preserve native hit testing on buttons. Point=" + point + " hit=" + WindowFromPoint(point) + " root=" + GetAncestor(WindowFromPoint(point), 2) + " expected=" + child.Handle + " bounds=" + child.Bounds + " target=" + Control.FromHandle(WindowFromPoint(point)));
                    var glass = typeof(PortableWindow).GetField("interactiveGlass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(child);
                    var front = (LockedGlass.GlassWindow)typeof(InteractiveGlass).GetField("foreground", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(glass);
                    Point before = front.Bounds.Location;
                    child.Location += new Size(20, 15);
                    Pump();
                    if (front.Bounds.Location != before + new Size(20, 15))
                        throw new Exception("Glass must follow window dragging.");
                    child.TopMost = false;
                    child.Hide();
                    Pump();
                    if (front.Visible)
                        throw new Exception("Hidden windows must hide their glass.");
                    TransparencyPreferences.Save(true, false, true, path);
                    Pump();
                    VerifyInteractive(main);
                    child.Show();
                    Pump();
                    if (child.Opacity != 1)
                        throw new Exception("Disabling child glass must restore full opacity.");
                    typeof(PortableWindow).GetMethod("SetOverlayLocked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, new object[] { true });
                    Pump();
                    TransparencyPreferences.Save(false, false, true, path);
                    Pump();
                    if (main.Opacity != 1 || !main.OverlayLocked)
                        throw new Exception("Disabling locked glass must retain lock behavior.");
                    typeof(PortableWindow).GetMethod("SetOverlayLocked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, new object[] { false });
                    Pump();
                    VerifyInteractive(main);
                    TransparencyPreferences.Load(path);
                    if (TransparencyPreferences.Locked || TransparencyPreferences.Children || !TransparencyPreferences.Main)
                        throw new Exception("Independent preferences must survive reload.");
                }
                using (var image = new Bitmap(20, 20))
                {
                    using (var graphics = Graphics.FromImage(image))
                        graphics.Clear(Theme.Background);
                    image.SetPixel(5, 5, Theme.TextColor);
                    image.SetPixel(6, 5, Theme.Muted);
                    image.SetPixel(2, 2, Color.FromArgb((Theme.Background.R + Theme.Surface.R) / 2, (Theme.Background.G + Theme.Surface.G) / 2, (Theme.Background.B + Theme.Surface.B) / 2));
                    using (var region = GlassFrame.Prepare(image, true))
                        if (!region.IsVisible(10, 10) || image.GetPixel(10, 10).A <= 60 || image.GetPixel(10, 10).A >= 160 || image.GetPixel(5, 5).A != 255 || image.GetPixel(2, 2).A > 130)
                            throw new Exception("Unlocked glass must keep its background frosted and text opaque.");
                    Color label = image.GetPixel(6, 5);
                    if (label.A != 255 || Math.Abs(label.GetBrightness() - Theme.TextColor.GetBrightness()) >= Math.Abs(Theme.Muted.GetBrightness() - Theme.TextColor.GetBrightness()))
                        throw new Exception("Glass labels must gain contrast without losing opacity.");
                }
            }
            finally { TransparencyPreferences.Save(oldLock, oldChildren, oldMain, path); }
        }
        private static void VerifyInteractive(PortableWindow window)
        {
            var glass = (InteractiveGlass)typeof(PortableWindow).GetField("interactiveGlass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
            if (glass == null)
                throw new Exception("Expected an interactive glass renderer.");
            if (!glass.Active)
            {
                if (window.Opacity != 1)
                    throw new Exception("Unsupported composition must stay opaque.");
                return;
            }
            if (window.Opacity <= 0 || window.Opacity >= 1 || window.OverlayLocked)
                throw new Exception("Interactive glass must keep a nonzero native hit surface without locking.");
            var front = (LockedGlass.GlassWindow)typeof(InteractiveGlass).GetField("foreground", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(glass);
            Point previousLocation = window.Location;
            window.Location = new Point(previousLocation.X + 9, previousLocation.Y + 7);
            if (front.Bounds.Location != window.PointToScreen(Point.Empty))
                throw new Exception("Glass foreground must move synchronously with its host.");
            window.Location = previousLocation;
            if (front.Bounds.Location != window.PointToScreen(Point.Empty))
                throw new Exception("Glass foreground must return to the original host position.");
            if (GetWindow(window.Handle, 2) != front.Handle)
                throw new Exception("Glass must follow its host in desktop z-order.");
        }
        private static void Pump()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 160)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(5);
            }
        }
    }
}
