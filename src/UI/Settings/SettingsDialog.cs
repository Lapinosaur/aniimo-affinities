using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class SettingsDialog : PortableWindow, IMessageFilter
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        private readonly KeyCapture[] fields = new KeyCapture[KeyBindings.Actions.Length];
        private readonly ApplicationTips tips = ApplicationTips.Create();
        internal Keys[] Result;
        internal SettingsDialog(Keys[] current, Func<int> mainType = null, Action startTutorial = null)
        {
            heightTransition.Tick += (s, e) => AdvanceHeight();
            VisibleChanged += (s, e) => { if (!Visible && heightTransition.Enabled) { heightTransition.Stop(); ClientSize = new Size(ClientSize.Width, heightTarget); } };
            Text = "Settings";
            ClientSize = new Size(460, 632);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(UiFonts.Own(new Label { Text = "Keyboard shortcuts", Font = UiFonts.Create("Segoe UI", 16, FontStyle.Bold), Size = new Size(282, 32), Location = new Point(24, 14) }));
            var appearance = new SettingsButton { Quiet = true, Location = new Point(396, 14), Size = new Size(36, 32), TabIndex = 0 };
            Action updateAppearance = () =>
            {
                string mode = Theme.Appearance == 0 ? "Dark" : Theme.Appearance == 1 ? "System" : "Light";
                appearance.AccessibleName = "Appearance: " + mode;
                appearance.Glyph = Theme.Appearance == 0 ? "moon" : Theme.Appearance == 1 ? "system" : "sun";
                tips.SetToolTip(appearance, "Appearance: " + mode + " · Click to switch");
                appearance.Invalidate();
            };
            appearance.Click += (s, e) => { Theme.CycleAppearance(); updateAppearance(); };
            Controls.Add(appearance);
            updateAppearance();
            Controls.Add(new Label { Text = "Click a shortcut and press a key to change it.", ForeColor = Theme.Muted, Size = new Size(412, 22), Location = new Point(24, 52) });
            Controls.Add(UiFonts.Own(new Label { Text = "ACTION", ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold), Size = new Size(180, 20), Location = new Point(28, 88) }));
            Controls.Add(UiFonts.Own(new Label { Text = "SHORTCUT", ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold), Size = new Size(160, 20), Location = new Point(224, 88) }));
            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (!displaySection)
                {
                    foreach (var field in fields.Where(f => f != null && f.Visible && f.TestHeld))
                        Drawing.Box(e.Graphics, new RectangleF(24, field.Top - 2, 412, field.Height + 4), Theme.AccentSurface, Theme.Accent, 7);
                    using (var line = new Pen(Theme.Border))
                        e.Graphics.DrawLine(line, 24, 616 - (windowSection ? (20 - KeyBindings.Actions.Length) * 38 : 28), 436, 616 - (windowSection ? (20 - KeyBindings.Actions.Length) * 38 : 28));
                }
            };
            var status = new Label { Location = new Point(24, 510), Size = new Size(412, 32), ForeColor = Theme.Muted };
            Controls.Add(status);
            for (int i = 0; i < fields.Length; i++)
            {
                int startCount = Controls.Count;
                int actionIndex = i;
                int rowIndex = i < 10 ? i : i - 10;
                int type = Array.IndexOf(Model.Names, KeyBindings.Actions[i]);
                var icon = new DrawingPanel { Location = new Point(32, 115 + rowIndex * 38), Size = new Size(30, 30), AccessibleName = KeyBindings.Actions[i], AccessibleRole = AccessibleRole.Graphic, TabStop = false };
                icon.Paint += (s, e) =>
                {
                    e.Graphics.Clear(icon.BackColor);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    if (type >= 0)
                        VectorIcon.Draw(e.Graphics, type, new RectangleF(3, 3, 24, 24));
                    else if (actionIndex == 10)
                        Glyphs.Draw(e.Graphics, "pin", new RectangleF(5, 5, 20, 20), Theme.Muted);
                    else if (actionIndex >= 11)
                        Glyphs.Draw(e.Graphics, actionIndex == 11 ? "lock" : actionIndex == 12 ? "settings" : actionIndex == 14 ? "matrix" : actionIndex == 15 ? "radar" : "minimize", new RectangleF(5, 5, 20, 20), Theme.Muted);
                    else
                        Glyphs.Draw(e.Graphics, "reset", new RectangleF(3, 3, 24, 24), Theme.Muted);
                };
                tips.SetToolTip(icon, KeyBindings.Actions[i]);
                Controls.Add(icon);
                var actionLabel = new Label { Text = i == 0 ? "Clear selection" : KeyBindings.Actions[i], Location = new Point(72, 115 + rowIndex * 38), Size = new Size(146, 30), TextAlign = ContentAlignment.MiddleLeft };
                Controls.Add(actionLabel);
                var field = new KeyCapture { Location = new Point(222, 116 + rowIndex * 38), Width = 174, AccessibleName = KeyBindings.Actions[i], TabIndex = i * 2, AllowModifiers = i >= 10, Binding = current[i] };
                field.Feedback = message => { status.ForeColor = field.Invalid ? Theme.Error : Theme.Muted; status.Text = message; };
                fields[i] = field;
                Controls.Add(field);
                tips.SetToolTip(field, "Rebind");
                field.CaptureEnded += (s, e) => ClearValidation();
                field.ValidateBinding = key =>
                {
                    ClearValidation();
                    var duplicate = fields.FirstOrDefault(item => item != null && item != field && item.Binding == key);
                    if (duplicate == null)
                        return true;
                    duplicate.ShowValidation(true, false);
                    status.Text = KeyBindings.Label(key) + " is used by " + duplicate.AccessibleName + ". Clear it there or try another key.";
                    status.ForeColor = Theme.Error;
                    tips.SetToolTip(field, "Already assigned to " + duplicate.AccessibleName);
                    tips.SetToolTip(duplicate, "Conflicts with " + field.AccessibleName);
                    Keys old = field.Binding;
                    using (var conflict = new ShortcutConflictWindow(KeyBindings.Label(key), field.AccessibleName, duplicate.AccessibleName, old == Keys.None ? "Unassigned" : KeyBindings.Label(old), duplicate.AllowModifiers || (old & Keys.Modifiers) == Keys.None))
                    {
                        conflict.ShowDialog(this);
                        field.Binding = old;
                        if (conflict.Choice > 0)
                        {
                            duplicate.Binding = conflict.Choice == 1 ? old : Keys.None;
                            field.Binding = key;
                            field.ShowValidation(false, true);
                            duplicate.ShowValidation(false, true);
                            status.ForeColor = Theme.Muted;
                            status.Text = "Shortcuts updated. Click Save to keep your changes.";
                        }
                        else
                        {
                            status.ForeColor = Theme.Muted;
                            status.Text = "Cancelled. Both shortcuts are unchanged.";
                        }
                    }
                    return false;
                };
                var clear = new SettingsButton { Quiet = true, Text = "×", AccessibleName = "Clear shortcut : " + KeyBindings.Actions[i], Location = new Point(404, 117 + rowIndex * 38), Size = new Size(26, 26), TabIndex = i * 2 + 1 };
                clear.TabStop = false;
                clearButtons[i] = clear;
                tips.SetToolTip(clear, "Clear shortcut · Del");
                clear.Click += (s, e) => { field.Focus(); field.Binding = Keys.None; status.Text = ""; };
                Controls.Add(clear);
                testRowFeedback[i] = held =>
                {
                    icon.BackColor = actionLabel.BackColor = held ? Theme.AccentSurface : Theme.Background;
                    actionLabel.ForeColor = held ? Theme.AccentForeground : Theme.TextColor;
                    clear.Surround = held ? Theme.AccentSurface : Color.Empty;
                    icon.Invalidate();
                    clear.Invalidate();
                };
                for (int c = startCount; c < Controls.Count; c++)
                    Controls[c].Tag = i >= 10 ? "window" : "types";
            }
            Controls.Add(UiFonts.Own(new Label { Text = "Delete clears the focused shortcut. Esc cancels.", ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8), Location = new Point(28, 492), Size = new Size(404, 18) }));
            var reset = ActionButton("Defaults", 24);
            var cancel = ActionButton("Cancel", 216);
            var save = ActionButton("Save", 330);
            save.Enabled = false;
            foreach (var field in fields)
                field.BindingChanged += (s, e) =>
                {
                    save.Enabled = !fields.Select(item => item.Binding).SequenceEqual(current);
                    RefreshNavigationHints();
                };
            reset.Click += (s, e) => { for (int i = 0; i < fields.Length; i++) fields[i].Binding = KeyBindings.Defaults()[i]; status.Text = ""; };
            cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel;
            AcceptButton = save;
            save.Click += (s, e) =>
            {
                var values = fields.Select(field => field.Binding).ToArray();
                if (!KeyBindings.Valid(values))
                {
                    status.Text = "This key is already assigned to another action.";
                    return;
                }
                try
                {
                    KeyBindings.Save(KeyBindings.FilePath, values);
                    Result = values;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (System.IO.IOException) { status.Text = "Unable to save. Your previous settings have been kept."; }
                catch (UnauthorizedAccessException) { status.Text = "Access to the preferences file was denied."; }
            };
            foreach (Control control in Controls)
                if (control.Top >= 88)
                    control.Top += 38;
            var typesTab = new SettingsButton { Text = "Types", Location = new Point(24, 82), Size = new Size(130, 30), TabIndex = 0, Primary = true };
            var windowTab = new SettingsButton { Text = "Window & overlay", Location = new Point(160, 82), Size = new Size(180, 30), TabIndex = 1 };
            var shortcutControls = Controls.Cast<Control>().Where(control => control.Top >= 126).ToArray();
            var editor = new DisplayDialog(this, mainType, () => fields.Select(field => field.Binding).ToArray()) { Location = new Point(0, 124), Visible = false, Size = new Size(440, 422) };
            var display = new SettingsButton { Text = "Display", Location = new Point(346, 82), Size = new Size(90, 30), TabIndex = 2 };
            var aboutPanel = new AboutPanel { Location = new Point(24, 124), Visible = false };
            var about = new SettingsButton { Glyph = "info", AccessibleName = "About & Credits", Location = new Point(312, 14), Size = new Size(34, 32), TabIndex = 1 };
            tips.SetToolTip(about, "About & Credits � Creator, links and acknowledgements");
            var guidePanel = new Panel { Location = new Point(24, 124), Size = new Size(412, 156), Visible = false };
            AddTutorialChoices(guidePanel, startTutorial);
            var guide = new SettingsButton { Glyph = "help", AccessibleName = "Tutorial", Location = new Point(354, 14), Size = new Size(34, 32), TabIndex = 1 };
            tips.SetToolTip(guide, "Help & Tutorial · Discord, diagnostics and guided tour");
            var heading = Controls.OfType<Label>().First(label => label.Text == "Keyboard shortcuts");
            heading.UseMnemonic = false;
            var description = Controls.OfType<Label>().First(label => label.Text == "Click a shortcut and press a key to change it.");
            var shortcutHint = Controls.OfType<Label>().Single(label => label.Text == "Delete clears the focused shortcut. Esc cancels.");
            status.TextChanged += (s, e) => { status.Visible = !displaySection && status.Text.Length > 0; shortcutHint.Visible = !displaySection && status.Text.Length == 0; };
            int fullHeight = ClientSize.Height + 36;
            bool layoutReady = false;
            var footerPositions = shortcutControls.Where(control => control.Top >= 530).ToDictionary(control => control, control => control.Top + 36);
            SettingsButton.Group(typesTab, windowTab, display);
            Action<bool> selectSection = window =>
            {
                if (layoutReady)
                {
                    int reduction = window ? (20 - KeyBindings.Actions.Length) * 38 : 28;
                    foreach (var item in footerPositions)
                        item.Key.Top = item.Value - (window ? reduction : item.Key is Button ? 28 : 0);
                    if (!window)
                        status.Top = shortcutHint.Top;
                    status.Height = 32;
                    TransitionHeight(fullHeight - reduction);
                }
                aboutPanel.Hide();
                about.Primary = false;
                about.Invalidate();
                guidePanel.Hide();
                guide.Primary = false;
                guide.Invalidate();
                displaySection = false;
                editor.Hide();
                display.Primary = false;
                display.Invalidate();
                heading.Text = "Keyboard shortcuts";
                description.Text = "Click a shortcut and press a key to change it.";
                foreach (var control in shortcutControls)
                    control.Visible = true;
                status.Visible = status.Text.Length > 0;
                shortcutHint.Visible = status.Text.Length == 0;
                windowSection = window;
                typesTab.Primary = !window;
                windowTab.Primary = window;
                foreach (Control control in Controls)
                    if (control.Tag is string)
                        control.Visible = (string)control.Tag == (window ? "window" : "types");
                typesTab.Invalidate();
                windowTab.Invalidate();
                Invalidate();
                RefreshNavigationHints();
            };
            typesTab.Click += (s, e) => selectSection(false);
            windowTab.Click += (s, e) => selectSection(true);
            Controls.Add(typesTab);
            Controls.Add(windowTab);
            selectSection(false);
            display.Click += (s, e) =>
            {
                aboutPanel.Hide();
                about.Primary = false;
                about.Invalidate();
                guidePanel.Hide();
                guide.Primary = false;
                guide.Invalidate();
                TransitionHeight(594);
                displaySection = true;
                foreach (var control in shortcutControls)
                    control.Visible = false;
                foreach (var hint in navigationHints)
                    if (hint != null)
                        hint.Visible = false;
                heading.Text = "Display";
                description.Text = "Window scale, text size and keyboard badges.";
                typesTab.Primary = false;
                windowTab.Primary = false;
                display.Primary = true;
                editor.Show();
                typesTab.Invalidate();
                windowTab.Invalidate();
                display.Invalidate();
                Invalidate();
            };
            Controls.Add(display);
            Controls.Add(editor);
            guide.Click += (s, e) =>
            {
                aboutPanel.Hide();
                about.Primary = false;
                about.Invalidate();
                TransitionHeight(720);
                displaySection = true;
                editor.Hide();
                foreach (var control in shortcutControls)
                    control.Visible = false;
                foreach (var hint in navigationHints)
                    if (hint != null)
                        hint.Visible = false;
                heading.Text = "Help & Tutorial";
                description.Text = "Learn the app, or get a hand from the community.";
                typesTab.Primary = false;
                windowTab.Primary = false;
                display.Primary = false;
                guide.Primary = true;

                guidePanel.Show();
                typesTab.Invalidate();
                windowTab.Invalidate();
                display.Invalidate();
                guide.Invalidate();
                Invalidate();
            };
            about.Click += (s, e) =>
            {
                TransitionHeight(730);
                displaySection = true;
                editor.Hide();
                guidePanel.Hide();
                foreach (var control in shortcutControls)
                    control.Visible = false;
                foreach (var hint in navigationHints)
                    if (hint != null)
                        hint.Visible = false;
                heading.Text = "About & Credits";
                description.Text = "Made by Lapinosaur. Built for the community.";
                foreach (var button in new[] { typesTab, windowTab, display, guide })
                {
                    button.Primary = false;
                    button.Invalidate();
                }
                about.Primary = true;
                about.Invalidate();
                aboutPanel.Show();
                Invalidate();
            };
            Controls.Add(aboutPanel);
            Controls.Add(about);
            Controls.Add(guidePanel);
            Controls.Add(guide);
            // Reserve the shared compact caption without overlapping the shortcut fields.
            foreach (Control control in Controls)
                control.Top += 36;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + 36);
            layoutReady = true;
            selectSection(false);
        }
        internal void TutorialSection(string name)
        {
            Controls.OfType<SettingsButton>().Single(button => button.Text == name).PerformClick();
            if (heightTransition.Enabled)
            {
                heightTransition.Stop();
                ClientSize = new Size(ClientSize.Width, heightTarget);
            }
        }
        private Button ActionButton(string text, int x)
        {
            var button = new SettingsButton { Text = text, Primary = text == "Save", Location = new Point(x, 552), Size = new Size(106, 34), TabIndex = 40 + Controls.OfType<Button>().Count() };
            Controls.Add(button);
            return button;
        }
        private void ClearValidation()
        {
            foreach (var field in fields.Where(item => item != null))
            {
                field.ShowValidation(false, false);
                tips.SetToolTip(field, "Rebind");
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                heightTransition.Dispose();
                if (inputFilterRegistered)
                    Application.RemoveMessageFilter(this);
                inputFilterRegistered = false;
                tips.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
