using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class AccessibilityTests
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Pump(int milliseconds)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < milliseconds)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(5);
            }
        }
        internal static void Run()
        {
            using (var form = new Form())
            {
                var plan = new TeamPlan();
                var buttons = new InteractiveButton[] { new SettingsButton { Text = "Save" }, new IconButton { Glyph = "help", AccessibleName = "Help" }, new SupportButton(), new TeamCard(plan, 0) { AccessibleName = "Ally 1" } };
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].SetBounds(0, i * 42, 180, 38);
                    form.Controls.Add(buttons[i]);
                }
                form.Show();
                Pump(20);
                foreach (var button in buttons)
                {
                    int clicks = 0;
                    button.Click += (s, e) => clicks++;
                    if (!button.TabStop || !button.Focus() || button.AccessibilityObject.Role != AccessibleRole.PushButton)
                        throw new Exception("Button is not keyboard/accessibility reachable: " + button.GetType().Name);
                    button.AccessibilityObject.DoDefaultAction();
                    if (clicks != 1)
                        throw new Exception("Accessible action did not activate exactly once.");
                    button.Enabled = false;
                    button.PerformClick();
                    button.AccessibilityObject.DoDefaultAction();
                    if (clicks != 1 || (button.AccessibilityObject.State & AccessibleStates.Unavailable) == 0)
                        throw new Exception("Disabled action remained available: " + button.GetType().Name);
                    button.Enabled = true;
                }
                var icon = (IconButton)buttons[1];
                icon.Active = true;
                if ((icon.AccessibilityObject.State & AccessibleStates.Selected) == 0)
                    throw new Exception("Type selection is not exposed.");
                var settings = (SettingsButton)buttons[0];
                settings.BeginBusy("Saving");
                if ((settings.AccessibilityObject.State & AccessibleStates.Busy) == 0)
                    throw new Exception("Busy state is not exposed.");
                settings.EndBusy();
            }
            using (var form = new Form())
            using (var hold = new HoldSizeButton())
            using (var capture = new KeyCapture { Binding = Keys.D1 })
            using (var name = new NameInput("Ally", "Name", "Ally name"))
            {
                form.Controls.AddRange(new Control[] { hold, capture, name });
                hold.Top = 60;
                name.Top = 100;
                form.Show();
                Pump(20);
                int commits = 0;
                hold.Commit += value => commits++;
                hold.Focus();
                typeof(Control).GetMethod("OnKeyDown", Flags).Invoke(hold, new object[] { new KeyEventArgs(Keys.Space) });
                hold.Enabled = false;
                Pump(1000);
                if (commits != 0)
                    throw new Exception("Disabling a held action must cancel its timer.");
                capture.AccessibilityObject.DoDefaultAction();
                if (!capture.Listening)
                    throw new Exception("Accessible shortcut action must start capture.");
                capture.Enabled = false;
                capture.ReleaseCapturedKey(Keys.D2);
                capture.AccessibilityObject.DoDefaultAction();
                if (capture.Listening || capture.Binding != Keys.D1)
                    throw new Exception("Disabled capture retained active input.");
                name.Enabled = false;
                if (name.Editor.Enabled || name.Editor.AccessibleName != "Ally name")
                    throw new Exception("Composite input lost native disabled state or name.");
            }
            foreach (int appearance in new[] { 0, 2 })
            {
                Theme.Appearance = appearance;
                Theme.ApplyAppearance();
                using (var form = new SettingsDialog(KeyBindings.Defaults()))
                {
                    form.Show();
                    Pump(20);
                    CheckNames(form);
                    form.TutorialSection("Display");
                    CheckNames(form);
                }
                using (var form = new MainForm())
                {
                    form.Show();
                    Pump(20);
                    CheckNames(form);
                }
            }
            Theme.Appearance = 1;
            Theme.ApplyAppearance();
            using (var editor = new TeamEditor(new TeamPlan(), () => { }))
            {
                editor.Show();
                Pump(20);
                var rows = editor.Controls.OfType<Panel>().Single(panel => panel.Controls.OfType<NameInput>().Any());
                var input = rows.Controls.OfType<NameInput>().OrderBy(control => control.TabIndex).First();
                input.Editor.Focus();
                typeof(TeamEditor).GetMethod("ProcessCmdKey", Flags).Invoke(editor, new object[] { Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.F6 });
                if (!rows.Controls.OfType<TeamTypeSelector>().OrderBy(control => control.TabIndex).First().ContainsFocus)
                    throw new Exception("F6 must provide keyboard entry to the element group without changing name Tab order.");
            }
        }
        private static void CheckNames(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child.Visible && (child is Button || child is KeyCapture || child is RadioButton) && string.IsNullOrWhiteSpace(child.AccessibleName) && string.IsNullOrWhiteSpace(child.Text))
                    throw new Exception("Unnamed interactive control: " + child.GetType().Name + " in " + parent.GetType().Name);
                CheckNames(child);
            }
        }
    }
}
