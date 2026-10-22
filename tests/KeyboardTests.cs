using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class KeyboardTests
    {
        internal static void Run()
        {
            using (var settings = new SettingsDialog(KeyBindings.Defaults()))
            {
                settings.Show();
                Application.DoEvents();
                var captures = settings.Controls.OfType<KeyCapture>().ToArray();
                settings.TestShortcut(Keys.D2, true);
                if (captures.Count(f => f.TestHeld) != 1 || !captures.Single(f => f.Binding == Keys.D2).TestHeld)
                    throw new Exception("Shortcut test must highlight only its assigned row");
                settings.TestShortcut(Keys.D2, false);
                settings.TestShortcut(Keys.NumPad3, true);
                if (!captures.Single(f => f.Binding == Keys.D3).TestHeld)
                    throw new Exception("Numpad shortcut test must normalize digits");
                settings.TestShortcut(Keys.NumPad3, false);
                if (captures.Any(f => f.TestHeld || f.Invalid) || !captures.Select(f => f.Binding).SequenceEqual(KeyBindings.Defaults()))
                    throw new Exception("Testing shortcuts must not alter bindings or leave an error");
                foreach (string choice in new[] { "Cancel", "Swap shortcuts", "Unbind other & assign" })
                {
                    var defaults = KeyBindings.Defaults();
                    for (int i = 0; i < captures.Length; i++)
                        captures[i].Binding = defaults[i];
                    var target = captures.Single(f => f.Binding == Keys.D1);
                    var other = captures.Single(f => f.Binding == Keys.D2);
                    target.Focus();
                    var handler = typeof(KeyCapture).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var message = Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero);
                    handler.Invoke(target, new object[] { message, Keys.Enter });
                    settings.BeginInvoke(new Action(() => Application.OpenForms.OfType<ShortcutConflictWindow>().Single().Controls.OfType<SettingsButton>().Single(b => b.Text == choice).PerformClick()));
                    handler.Invoke(target, new object[] { message, Keys.D2 });
                    if (target.Binding != (choice == "Cancel" ? Keys.D1 : Keys.D2) || other.Binding != (choice == "Cancel" ? Keys.D2 : choice == "Swap shortcuts" ? Keys.D1 : Keys.None))
                        throw new Exception("Conflict resolution failed: " + choice);
                    if (!KeyBindings.Valid(captures.Select(f => f.Binding).ToArray()))
                        throw new Exception("Conflict resolution produced invalid bindings");
                }
                settings.Close();
            }
            var custom = KeyBindings.Defaults();
            custom[1] = Keys.Q;
            var unbound = KeyBindings.Defaults();
            unbound[1] = Keys.None;
            unbound[2] = Keys.None;
            if (!KeyBindings.Valid(unbound) || KeyBindings.Resolve(unbound, Keys.D1) != -1 || KeyBindings.Resolve(unbound, Keys.None) != -1 || KeyBindings.Label(Keys.None) != "")
                throw new Exception("Unbind failed");
            using (var capture = new KeyCapture())
            {
                var handler = typeof(KeyCapture).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var keyMessage = Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero);
                capture.Binding = Keys.D1;
                handler.Invoke(capture, new object[] { keyMessage, Keys.Space });
                handler.Invoke(capture, new object[] { keyMessage, Keys.Escape });
                handler.Invoke(capture, new object[] { keyMessage, Keys.R });
                if (capture.Binding != Keys.D1)
                    throw new Exception("Escape did not cancel capture");
                handler.Invoke(capture, new object[] { keyMessage, Keys.R });
                if (capture.Binding != Keys.D1)
                    throw new Exception("Inactive field captured a key");
                handler.Invoke(capture, new object[] { keyMessage, Keys.Space });
                handler.Invoke(capture, new object[] { keyMessage, Keys.R });
                capture.ReleaseCapturedKey(Keys.R);
                if (capture.Binding != Keys.R)
                    throw new Exception("Key capture failed");
                capture.ValidateBinding = key => key != Keys.D2;
                handler.Invoke(capture, new object[] { keyMessage, Keys.Space });
                handler.Invoke(capture, new object[] { keyMessage, Keys.D2 });
                if (capture.Binding != Keys.R || !capture.Invalid)
                    throw new Exception("Duplicate binding was not rejected visually");
                handler.Invoke(capture, new object[] { keyMessage, Keys.R });
                capture.ReleaseCapturedKey(Keys.R);
                if (!capture.CaptureValidated || capture.Invalid)
                    throw new Exception("Valid binding did not clear rejection");
                handler.Invoke(capture, new object[] { keyMessage, Keys.T });
                if (capture.Binding != Keys.R)
                    throw new Exception("Capture remained active after assignment");
                handler.Invoke(capture, new object[] { keyMessage, Keys.Space });
                handler.Invoke(capture, new object[] { keyMessage, Keys.Control | Keys.T });
                if (capture.Binding != Keys.R)
                    throw new Exception("Unsupported chord changed binding");
                handler.Invoke(capture, new object[] { keyMessage, Keys.Delete });
                if (capture.Binding != Keys.None)
                    throw new Exception("Delete did not unbind");
            }
            using (var capture = new KeyCapture { AllowModifiers = true, Binding = Keys.D1 })
            {
                var handler = typeof(KeyCapture).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var message = Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero);
                handler.Invoke(capture, new object[] { message, Keys.Space });
                handler.Invoke(capture, new object[] { message, Keys.Control | Keys.F });
                capture.ReleaseCapturedKey(Keys.F);
                if (capture.Binding != Keys.D1)
                    throw new Exception("Chord committed before Ctrl release");
                capture.ReleaseCapturedKey(Keys.ControlKey);
                if (capture.Binding != (Keys.Control | Keys.F))
                    throw new Exception("Released chord not captured");
                handler.Invoke(capture, new object[] { message, Keys.Space });
                handler.Invoke(capture, new object[] { message, Keys.Control | Keys.G });
                capture.ReleaseCapturedKey(Keys.ControlKey);
                if (capture.Binding != (Keys.Control | Keys.F))
                    throw new Exception("Chord committed before primary release");
                capture.ReleaseCapturedKey(Keys.G);
                if (capture.Binding != (Keys.Control | Keys.G))
                    throw new Exception("Reverse release order failed");
                handler.Invoke(capture, new object[] { message, Keys.Space });
                handler.Invoke(capture, new object[] { message, Keys.Control | Keys.H });
                handler.Invoke(capture, new object[] { message, Keys.Escape });
                capture.ReleaseCapturedKey(Keys.H);
                capture.ReleaseCapturedKey(Keys.ControlKey);
                if (capture.Binding != (Keys.Control | Keys.G))
                    throw new Exception("Pending chord not cancelled");
                handler.Invoke(capture, new object[] { message, Keys.Space });
                handler.Invoke(capture, new object[] { message, Keys.NumPad3 });
                capture.ReleaseCapturedKey(Keys.NumPad3);
                if (capture.Binding != Keys.D3)
                    throw new Exception("Numpad release failed");
                handler.Invoke(capture, new object[] { message, Keys.Enter });
                handler.Invoke(capture, new object[] { message, Keys.Control | Keys.ControlKey });
                capture.ReleaseCapturedKey(Keys.ControlKey);
                handler.Invoke(capture, new object[] { message, Keys.J });
                capture.ReleaseCapturedKey(Keys.J);
                if (capture.Binding != Keys.J)
                    throw new Exception("Enter capture and released modifier must allow a plain key");
            }
            if (WindowZoom.Step(1, 1) != 1.25f || WindowZoom.Step(1, -1) != .75f || WindowZoom.Step(.5f, -1) != .5f || WindowZoom.Step(1.5f, 1) != 1.5f)
                throw new Exception("Window scale stepping failed");
            if (KeyBindings.Resolve(custom, Keys.Q) != 1 || KeyBindings.Resolve(custom, Keys.D1) != -1 || KeyBindings.Resolve(custom, Keys.Control | Keys.Q) != -1)
                throw new Exception("Rebinding failed");
            var duplicate = (Keys[])custom.Clone();
            duplicate[2] = Keys.Q;
            if (KeyBindings.Valid(duplicate))
                throw new Exception("Duplicate binding accepted");
            var config = Path.Combine(Path.GetTempPath(), "aniimo-keys-" + Guid.NewGuid().ToString("N"), "keys.txt");
            try
            {
                KeyBindings.Save(config, custom);
                if (!KeyBindings.Load(config).SequenceEqual(custom))
                    throw new Exception("Bindings did not persist");
                KeyBindings.Save(config, unbound);
                if (!KeyBindings.Load(config).SequenceEqual(unbound))
                    throw new Exception("Unbound bindings did not persist");
                KeyBindings.Save(config, KeyBindings.Defaults());
                if (!KeyBindings.Load(config).SequenceEqual(KeyBindings.Defaults()))
                    throw new Exception("Defaults did not persist");
                var legacy = KeyBindings.Defaults().Take(15).ToArray();
                legacy[1] = Keys.Q;
                File.WriteAllLines(config, legacy.Select(key => ((int)key).ToString()).ToArray());
                var upgraded = KeyBindings.Load(config);
                if (!upgraded.Take(15).SequenceEqual(legacy) || upgraded[15] != Keys.F10)
                    throw new Exception("Radar shortcut migration lost bindings");
                legacy[1] = Keys.F10;
                File.WriteAllLines(config, legacy.Select(key => ((int)key).ToString()).ToArray());
                if (KeyBindings.Load(config)[15] != Keys.None)
                    throw new Exception("Radar migration duplicated a binding");
                File.WriteAllText(config, "invalid");
                if (!KeyBindings.Load(config).SequenceEqual(KeyBindings.Defaults()))
                    throw new Exception("Invalid config fallback");
            }
            finally { if (File.Exists(config)) File.Delete(config); Directory.Delete(Path.GetDirectoryName(config)); }
            using (var keyboard = new MainForm())
            {
                typeof(MainForm).GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(keyboard, KeyBindings.Defaults());
                keyboard.Show();
                Application.DoEvents();
                var method = typeof(MainForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var field = typeof(MainForm).GetField("selection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var state = (Selection)field.GetValue(keyboard);
                Action<Keys, bool> press = (key, repeat) =>
                {
                    var message = Message.Create(keyboard.Handle, 0x100, new IntPtr((int)(key & Keys.KeyCode)), new IntPtr(repeat ? 1L << 30 : 0));
                    method.Invoke(keyboard, new object[] { message, key });
                };
                string[] order = { "Dark", "Earth", "Fire", "Grass", "Ice", "Light", "Lightning", "Water", "Wind" };
                for (int i = 1; i <= 9; i++)
                {
                    press(Keys.D0, false);
                    press((Keys)((int)Keys.D0 + i), false);
                    int expected = Array.IndexOf(Model.Names, order[i - 1]);
                    if (state.Main != expected || state.Sub != -1)
                        throw new Exception("Digit shortcut mismatch");
                    press((Keys)((int)Keys.D0 + i), true);
                    if (state.Main != expected)
                        throw new Exception("Repeated shortcut toggled");
                    press(Keys.Control | Keys.D0, false);
                    if (state.Main != expected)
                        throw new Exception("Ctrl shortcut intercepted");
                    press(Keys.D0, false);
                    press((Keys)((int)Keys.NumPad0 + i), false);
                    if (state.Main != expected)
                        throw new Exception("Numpad shortcut mismatch");
                    press(Keys.D0, false);
                    press((Keys)((int)Keys.D0 + i) | Keys.Shift, false);
                    if (state.Main != expected)
                        throw new Exception("Shift number row mismatch");
                }
                var row = (Panel)typeof(MainForm).GetField("row", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(keyboard);
                var controls = row.Controls.OfType<IconButton>().OrderBy(button => button.TabIndex).ToArray();
                controls[0].Focus();
                press(Keys.Left, false);
                if (!controls[9].Focused)
                    throw new Exception("Left wrap failed");
                press(Keys.Right, false);
                if (!controls[0].Focused)
                    throw new Exception("Right wrap failed");
                press(Keys.End, false);
                if (!controls[9].Focused)
                    throw new Exception("End failed");
                press(Keys.Enter, false);
                if (state.Main != -1)
                    throw new Exception("Enter reset failed");
                if (controls[9].Visible)
                    throw new Exception("Clear must hide for an empty selection");
                controls[0].Focus();
                press(Keys.Left, false);
                if (!controls[8].Focused)
                    throw new Exception("Empty navigation included hidden clear");
                keyboard.Close();
            }
            using (var early = new MainForm())
            {
                early.Show();
                early.Close();
                Application.DoEvents();
                if (!early.IsDisposed)
                    throw new Exception("Early close failed");
            }
            using (var ready = new MainForm())
            {
                ready.Show();
                Application.DoEvents();
                ready.SetPinned(true);
                if (!ready.TopMost || ready.TransparencyKey != Theme.Background || ready.TransparencyKey == Theme.Surface)
                    throw new Exception("Pinned overlay must clear the background and retain cards");
                ready.SetPinned(false);
                if (ready.TopMost || ready.TransparencyKey != Color.Empty)
                    throw new Exception("Unpin must restore the opaque window");
                var close = ((Panel)typeof(PortableWindow).GetField("captionPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ready)).Controls.OfType<Button>().SingleOrDefault(button => button.AccessibleName == "Close (Alt+F4)");
                if (close == null || ready.FormBorderStyle != FormBorderStyle.None)
                    throw new Exception("Caption not ready");
                close.PerformClick();
                if (!ready.IsDisposed)
                    throw new Exception("Custom close failed");
            }
            if (Model.Names.Length != 9)
                throw new Exception("Expected nine types");
            for (int a = 0; a < 9; a++)
                for (int b = -1; b < 9; b++)
                {
                    var values = Model.Factors(a, b);
                    if (values.Length != 18 || values.Any(v => double.IsNaN(v) || v <= 0))
                        throw new Exception("Invalid matchup");
                }
        }
    }
}
