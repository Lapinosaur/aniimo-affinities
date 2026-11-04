using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class SupportTests
    {
        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr window);
        private static void OpenFrom(Control control)
        {
            var message = Message.Create(control.Handle, 0x100, new IntPtr((int)Keys.F11), IntPtr.Zero);
            if (!new SupportShortcut().PreFilterMessage(ref message))
                throw new Exception("F11 must work from " + control.GetType().Name);
        }
        internal static void Run()
        {
            string path = Path.Combine(Path.GetTempPath(), "affinisaur-support-keys-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                var keys = KeyBindings.Defaults();
                keys[0] = Keys.F11;
                keys[1] = Keys.F12;
                File.WriteAllLines(path, keys.Select(key => ((int)key).ToString()).ToArray());
                var migrated = KeyBindings.Load(path);
                if (migrated[0] != Keys.F5 || migrated[1] != Keys.F12 || !migrated.Skip(2).SequenceEqual(keys.Skip(2)) || !KeyBindings.Valid(migrated))
                    throw new Exception("F11 migration must select a free key without resetting other bindings.");
                KeyBindings.Save(path, migrated);
                if (!KeyBindings.Load(path).SequenceEqual(migrated) || KeyBindings.IsChoice(Keys.F11))
                    throw new Exception("F11 must remain reserved after saving the migrated map.");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
            using (var parent = new MainForm())
            {
                parent.Show();
                var filter = new SupportShortcut();
                var message = Message.Create(parent.Handle, 0x100, new IntPtr((int)Keys.F11), IntPtr.Zero);
                if (!filter.PreFilterMessage(ref message))
                    throw new Exception("F11 must open Support.");
                var original = Application.OpenForms.OfType<SupportWindow>().Single();
                var handle = original.Handle;
                var bounds = original.Bounds;
                int enabledChanges = 0;
                original.EnabledChanged += (s, e) => enabledChanges++;
                filter.PreFilterMessage(ref message);
                OpenFrom(original);
                if (original.Handle != handle || original.Bounds != bounds || enabledChanges != 0)
                    throw new Exception("Repeated F11 must focus Support without rebuilding or disabling it.");
                var windows = Application.OpenForms.OfType<SupportWindow>().ToArray();
                if (windows.Length != 1 || windows[0].Modal || !parent.Enabled)
                    throw new Exception("Support must reuse one non-blocking window.");
                parent.Close();
                if (!windows[0].IsDisposed)
                    throw new Exception("Support must close with its owner.");
            }
            using (var parent = new MainForm())
            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            {
                parent.Show();
                OpenFrom(parent);
                var support = Application.OpenForms.OfType<SupportWindow>().Single();
                Exception failure = null;
                settings.Shown += (s, e) => settings.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        OpenFrom(settings);
                        if (support.Owner != settings || !IsWindowEnabled(support.Handle) || IsWindowEnabled(parent.Handle))
                            throw new Exception("F11 must restore Support during a modal dialog without enabling the blocked parent.");
                        var capture = settings.Controls.OfType<KeyCapture>().First();
                        Keys before = capture.Binding;
                        typeof(KeyCapture).GetMethod("BeginCapture", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(capture, null);
                        OpenFrom(capture);
                        if (capture.Listening || capture.Binding != before || Application.OpenForms.OfType<SupportWindow>().Count() != 1)
                            throw new Exception("F11 must cancel capture safely and open the same Support window.");
                    }
                    catch (Exception error) { failure = error; }
                    finally { settings.Close(); }
                }));
                settings.ShowDialog(parent);
                if (failure != null)
                    throw failure;
            }
            using (var source = new MainForm())
            {
                Form[] hosts = { new OnboardingWindow(), new RadarDialog(0, 1, KeyBindings.Defaults()), new MatrixDialog(-1, KeyBindings.Defaults()), new UpdateWindow(new ReleaseUpdate { Version = "Demo", Changes = new ReleaseChange[0] }), new TutorialOverlay(source) };
                foreach (var host in hosts)
                    using (host)
                    {
                        host.Show();
                        OpenFrom(host);
                        var support = Application.OpenForms.OfType<SupportWindow>().Single();
                        if (support.Owner != host || !support.Visible || !IsWindowEnabled(support.Handle))
                            throw new Exception("Support must be usable from " + host.GetType().Name);
                        host.Close();
                    }
            }
        }
    }
}
