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
            Font = UiFonts.Create("Segoe UI", 9);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(new Label { Text = "Keyboard shortcuts", Font = UiFonts.Create("Segoe UI", 16, FontStyle.Bold), Size = new Size(350, 32), Location = new Point(24, 14) });
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
            Controls.Add(new Label { Text = "ACTION", ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold), Size = new Size(180, 20), Location = new Point(28, 88) });
            Controls.Add(new Label { Text = "SHORTCUT", ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold), Size = new Size(160, 20), Location = new Point(224, 88) });
            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (!displaySection)
                {
                    for (int row = 0; row < (windowSection ? KeyBindings.Actions.Length - 10 : 10); row++)
                        Drawing.Box(e.Graphics, new RectangleF(24, 186 + row * 38, 412, 36), row % 2 == 0 ? Theme.Surface : Theme.Background, Theme.Background, 7);
                    using (var line = new Pen(Theme.Border))
                        e.Graphics.DrawLine(line, 24, 616 - (windowSection ? (20 - KeyBindings.Actions.Length) * 38 : 28), 436, 616 - (windowSection ? (20 - KeyBindings.Actions.Length) * 38 : 28));
                }
            };
            var status = new Label { Location = new Point(24, 510), Size = new Size(412, 32), ForeColor = Theme.Error };
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
                    e.Graphics.Clear(Theme.Background);
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
                Controls.Add(new Label { Text = i == 0 ? "Clear selection" : KeyBindings.Actions[i], Location = new Point(72, 115 + rowIndex * 38), Size = new Size(146, 30), TextAlign = ContentAlignment.MiddleLeft });
                var field = new KeyCapture { Location = new Point(222, 116 + rowIndex * 38), Width = 174, AccessibleName = KeyBindings.Actions[i], TabIndex = i * 2, AllowModifiers = i >= 10, Binding = current[i] };
                field.Feedback = message => status.Text = message;
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
                    tips.SetToolTip(field, "Already assigned to " + duplicate.AccessibleName);
                    tips.SetToolTip(duplicate, "Conflicts with " + field.AccessibleName);
                    return false;
                };
                var clear = new SettingsButton { Quiet = true, Text = "×", AccessibleName = "Clear shortcut : " + KeyBindings.Actions[i], Location = new Point(404, 117 + rowIndex * 38), Size = new Size(26, 26), TabIndex = i * 2 + 1 };
                clear.TabStop = false;
                clearButtons[i] = clear;
                tips.SetToolTip(clear, "Clear shortcut · Del");
                clear.Click += (s, e) => { field.Focus(); field.Binding = Keys.None; status.Text = ""; };
                Controls.Add(clear);
                for (int c = startCount; c < Controls.Count; c++)
                    Controls[c].Tag = i >= 10 ? "window" : "types";
            }
            Controls.Add(new Label { Text = "Delete clears the focused shortcut. Esc cancels.", ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8), Location = new Point(28, 492), Size = new Size(404, 18) });
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
            var guidePanel = new Panel { Location = new Point(24, 124), Size = new Size(412, 156), Visible = false };
            var guideText = new Label { Text = "Discover the controls, views and overlay in a guided tour.", Location = new Point(0, 8), Size = new Size(412, 38), ForeColor = Theme.Muted };
            var hideTour = new CheckBox { Text = "Don't show this tutorial again", Checked = TutorialPreferences.Hidden, Location = new Point(0, 52), Size = new Size(412, 28), ForeColor = Theme.TextColor, TabIndex = 0 };
            var guideStatus = new Label { Location = new Point(0, 82), Size = new Size(412, 24), ForeColor = Theme.Error };
            hideTour.CheckedChanged += (s, e) =>
            {
                try
                {
                    TutorialPreferences.Save(hideTour.Checked);
                    guideStatus.Text = "";
                }
                catch (System.IO.IOException) { guideStatus.Text = "Unable to save this preference."; }
                catch (UnauthorizedAccessException) { guideStatus.Text = "Unable to save this preference."; }
            };
            var launchTour = new SettingsButton { Text = "Start tutorial", Primary = true, Location = new Point(260, 112), Size = new Size(152, 34), TabIndex = 1 };
            launchTour.Click += (s, e) => { if (startTutorial != null) { startTutorial(); hideTour.Checked = TutorialPreferences.Hidden; } else guideStatus.Text = "Open the tutorial from the main window's Settings."; };
            guidePanel.Controls.Add(guideText);
            guidePanel.Controls.Add(hideTour);
            guidePanel.Controls.Add(guideStatus);
            guidePanel.Controls.Add(launchTour);
            var guide = new SettingsButton { Glyph = "help", AccessibleName = "Tutorial", Location = new Point(354, 14), Size = new Size(34, 32), TabIndex = 1 };
            tips.SetToolTip(guide, "Tutorial · Replay and startup preference");
            var heading = Controls.OfType<Label>().First(label => label.Text == "Keyboard shortcuts");
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
                    status.Height = window ? 32 : 24;
                    TransitionHeight(fullHeight - reduction);
                }
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
                TransitionHeight(330);
                displaySection = true;
                editor.Hide();
                foreach (var control in shortcutControls)
                    control.Visible = false;
                foreach (var hint in navigationHints)
                    if (hint != null)
                        hint.Visible = false;
                heading.Text = "Tutorial";
                description.Text = "Learn the app at your own pace.";
                typesTab.Primary = false;
                windowTab.Primary = false;
                display.Primary = false;
                guide.Primary = true;
                hideTour.Checked = TutorialPreferences.Hidden;
                guidePanel.Show();
                typesTab.Invalidate();
                windowTab.Invalidate();
                display.Invalidate();
                guide.Invalidate();
                Invalidate();
            };
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
