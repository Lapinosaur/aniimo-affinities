using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class DisplayDialog : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        private float scale = DisplaySettings.FontScale, windowScale = DisplaySettings.WindowScale;
        private int appearance = Theme.Appearance, appliedAppearance = Theme.Appearance;
        private void EndThemePreview(object sender, EventArgs e)
        {
            Theme.EndAppearancePreview(this);
        }
        private PortableWindow activeGlassPreview;
        private void PreviewGlass(int kind, bool enabled)
        {
            PortableWindow target = settingsHost as PortableWindow ?? this;
            if (kind == 2)
            {
                Form parent = target.Owner;
                while (parent != null && !(parent is MainForm))
                    parent = parent.Owner;
                target = parent as MainForm;
            }
            if (!ReferenceEquals(activeGlassPreview, target))
                EndGlassPreview(null, EventArgs.Empty);
            activeGlassPreview = target;
            if (target != null)
                target.PreviewWindowGlass(this, enabled);
        }
        private void EndGlassPreview(object sender, EventArgs e)
        {
            var target = activeGlassPreview;
            activeGlassPreview = null;
            if (target != null)
                target.EndWindowGlassPreview(this);
        }
        private Panel themeChoices;
        private void SyncAppearance()
        {
            if (Theme.PreviewingAppearance || appliedAppearance == Theme.Appearance)
                return;
            appliedAppearance = Theme.Appearance;
            appearance = Theme.Appearance;
            if (themeChoices != null)
                foreach (ThemePreview choice in themeChoices.Controls)
                    choice.Checked = choice.Mode == appearance;
        }
        private bool badges = DisplaySettings.ShowBadges;
        private readonly Form settingsHost;
        private Panel zoomChoices;
        private readonly System.Collections.Generic.List<TransparencyPreview> transparencyPreviews = new System.Collections.Generic.List<TransparencyPreview>();
        private void SyncZoom()
        {
            windowScale = DisplaySettings.WindowScale;
            if (zoomChoices != null)
                foreach (DisplayChoice choice in zoomChoices.Controls)
                    choice.Checked = Math.Abs(choice.PreviewScale - windowScale) < .01;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisplaySettings.DisplayChanged -= SyncZoom;
                Theme.Changed -= SyncAppearance;
                if (settingsHost != null)
                    settingsHost.Deactivate -= EndThemePreview;
                if (settingsHost != null)
                    settingsHost.Deactivate -= EndGlassPreview;
                EndGlassPreview(null, EventArgs.Empty);
                Theme.EndAppearancePreview(this);
                foreach (var preview in transparencyPreviews)
                    preview.Dispose();
            }
            base.Dispose(disposing);
        }
        protected override void OnShown(EventArgs e)
        {
            if (settingsHost == null)
                base.OnShown(e);
        }
        protected override bool ProcessCmdKey(ref Message message, Keys input)
        {
            if (settingsHost != null && input == Keys.Escape)
            {
                settingsHost.Close();
                return true;
            }
            return base.ProcessCmdKey(ref message, input);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394", Justification = "Random selects a cosmetic preview icon only; it does not generate secrets, identifiers or security decisions.")]
        internal DisplayDialog(Form host = null, Func<int> mainType = null, Func<Keys[]> previewBindings = null, Action lockTutorial = null)
        {
            settingsHost = host;
            VisibleChanged += (s, e) => { if (!Visible) Theme.EndAppearancePreview(this); };
            VisibleChanged += (s, e) => { if (!Visible) EndGlassPreview(null, EventArgs.Empty); };
            Deactivate += EndGlassPreview;
            if (settingsHost != null)
                settingsHost.Deactivate += EndGlassPreview;
            Deactivate += EndThemePreview;
            if (settingsHost != null)
                settingsHost.Deactivate += EndThemePreview;
            int fallbackType = new Random().Next(Model.Names.Length);
            Func<int> previewType = () => { int selected = mainType == null ? -1 : mainType(); return selected >= 0 && selected < Model.Names.Length ? selected : fallbackType; };
            if (previewBindings == null)
            {
                var savedBindings = KeyBindings.Load(KeyBindings.FilePath);
                previewBindings = () => savedBindings;
            }
            Text = "Display settings";
            ClientSize = new Size(440, 460);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(new Label { Text = "Text size", ForeColor = Theme.Muted, Location = new Point(24, 46), Size = new Size(220, 18) });
            for (int i = 0; i < 3; i++)
            {
                float value = 0.9f + i * 0.1f;
                var preview = new DisplayChoice
                {
                    Text = new[] { "Small · 90%", "Default · 100%", "Large · 110%" }[i],
                    PreviewScale = value,
                    Location = new Point(24 + i * 132, 66),
                    Size = new Size(128, 100),
                    TabIndex = i + 1,
                    Checked = Math.Abs(scale - value) < .01,
                    AccessibleName = "Text size: " + Math.Round(value * 100) + " percent"
                };
                preview.CheckedChanged += (s, e) => { if (preview.Checked) scale = value; };
                Controls.Add(preview);
            }
            Controls.Add(new Label { Text = "Keyboard badges", ForeColor = Theme.Muted, Location = new Point(24, 280), Size = new Size(200, 22) });
            var badgeChoices = new Panel { Location = new Point(224, 268), Size = new Size(192, 72), TabIndex = 8, AccessibleName = "Keyboard badge visibility" };
            Controls.Add(badgeChoices);
            for (int i = 0; i < 2; i++)
            {
                bool visible = i == 0;
                var choice = new DisplayChoice
                {
                    Text = visible ? "Shown" : "Hidden",
                    BadgePreview = true,
                    PreviewBadges = visible,
                    PreviewType = previewType,
                    PreviewBindings = previewBindings,
                    Location = new Point(i * 98, 0),
                    Size = new Size(94, 72),
                    TabIndex = i,
                    Checked = badges == visible,
                    AccessibleName = visible ? "Show keyboard badges" : "Hide keyboard badges"
                };
                choice.CheckedChanged += (s, e) => { if (choice.Checked) badges = visible; };
                badgeChoices.Controls.Add(choice);
            }
            var status = new Label { ForeColor = Theme.Error, Location = new Point(24, 376), Size = new Size(392, 22) };
            Controls.Add(status);
            var cancel = new SettingsButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(196, 400), Size = new Size(106, 34), TabIndex = 4 };
            var save = new SettingsButton { Text = "Save", Primary = true, Location = new Point(310, 400), Size = new Size(106, 34), TabIndex = 5 };
            Controls.Add(cancel);
            Controls.Add(save);
            CancelButton = cancel;
            AcceptButton = save;
            Controls.Add(new Label { Text = "Window scale", ForeColor = Theme.Muted, Location = new Point(24, 172), Size = new Size(392, 18) });
            var windowChoices = new Panel { Location = new Point(24, 192), Size = new Size(392, 72), TabIndex = 4, AccessibleName = "Window size" };
            Controls.Add(windowChoices);
            zoomChoices = windowChoices;
            DisplaySettings.DisplayChanged += SyncZoom;
            for (int i = 0; i < 5; i++)
            {
                float zoom = .5f + i * .25f;
                var choice = new DisplayChoice
                {
                    Text = Math.Round(zoom * 100) + "%",
                    PreviewScale = zoom,
                    WindowPreview = true,
                    Location = new Point(i * 79, 0),
                    Size = new Size(76, 72),
                    TabIndex = i,
                    Checked = Math.Abs(windowScale - zoom) < .01,
                    AccessibleName = "Window size: " + Math.Round(zoom * 100) + " percent"
                };
                choice.CheckedChanged += (s, e) => { if (choice.Checked) windowScale = zoom; };
                windowChoices.Controls.Add(choice);
            }
            windowChoices.Height = 56;
            foreach (Control choice in windowChoices.Controls)
                choice.Height = 56;
            badgeChoices.Top = 252;
            badgeChoices.Height = 60;
            foreach (Control choice in badgeChoices.Controls)
                choice.Height = 60;
            foreach (Control control in Controls)
                if (control is Label && control.Text == "Keyboard badges")
                    control.Top = 264;
            themeChoices = new Panel { Bounds = new Rectangle(24, 318, 392, 44), TabIndex = 9, AccessibleName = "Theme previews" };
            for (int i = 0; i < 3; i++)
            {
                int mode = i;
                var choice = new ThemePreview { Mode = mode, Text = new[] { "Dark", "System", "Light" }[i], Bounds = new Rectangle(i * 132, 0, 128, 44), TabIndex = i, Checked = appearance == mode };
                choice.AccessibleName = "Theme: " + choice.Text;
                choice.AccessibleDescription = "Hover to preview on the window. Select then Apply to save.";
                choice.MouseEnter += (s, e) => Theme.PreviewAppearance(this, mode);
                choice.MouseLeave += EndThemePreview;
                choice.CheckedChanged += (s, e) => { if (choice.Checked) appearance = mode; };
                themeChoices.Controls.Add(choice);
            }
            Controls.Add(themeChoices);
            Theme.Changed += SyncAppearance;
            Controls.Add(new Label { Text = "Frosted glass", ForeColor = Theme.Muted, Bounds = new Rectangle(24, 346, 220, 18) });
            var lockGlass = new RoundCheckBox { Text = "Locked overlay", Checked = TransparencyPreferences.Locked, AccessibleDescription = "Enabled by default. Spaces between locked panels stay transparent.", Bounds = new Rectangle(24, 366, 128, 24), TabIndex = 9 };
            var lockHelp = new SettingsButton { Glyph = "help", AccessibleName = "Lock tutorial", Bounds = new Rectangle(126, 422, 26, 26), TabIndex = 10, Enabled = lockTutorial != null };
            var childGlass = new RoundCheckBox { Text = "Secondary", AccessibleName = "Secondary windows", Checked = TransparencyPreferences.Children, AccessibleDescription = "Frosted glass in settings, downloads and other secondary windows.", Bounds = new Rectangle(156, 366, 128, 24), TabIndex = 11 };
            var mainGlass = new RoundCheckBox { Text = "Main window", Checked = TransparencyPreferences.Main, AccessibleDescription = "Frosted glass in the unlocked main window; text and icons stay sharp.", Bounds = new Rectangle(288, 366, 128, 24), TabIndex = 12 };
            lockHelp.Click += (s, e) => { if (lockTutorial != null) lockTutorial(); };
            Controls.Add(lockHelp);
            var glassOptions = new[] { lockGlass, childGlass, mainGlass };
            for (int i = 0; i < glassOptions.Length; i++)
            {
                if (string.IsNullOrEmpty(glassOptions[i].AccessibleName))
                    glassOptions[i].AccessibleName = glassOptions[i].Text;
                var preview = new TransparencyPreview(glassOptions[i], i) { Bounds = new Rectangle(24 + i * 132, 366, 128, 74), TabIndex = glassOptions[i].TabIndex };
                int kind = i;
                preview.MouseEnter += (s, e) => PreviewGlass(kind, preview.PreviewEnabled);
                preview.MouseLeave += EndGlassPreview;
                preview.CheckedChanged += (s, e) => { if (preview.Inverse) PreviewGlass(kind, preview.PreviewEnabled); };
                transparencyPreviews.Add(preview);
                Controls.Add(preview);
                var option = glassOptions[i];
                option.MouseEnter += (s, e) => preview.Inverse = true;
                option.MouseLeave += (s, e) => preview.Inverse = false;
            }
            lockHelp.Parent = transparencyPreviews[0];
            lockHelp.Location = new Point(transparencyPreviews[0].Width - lockHelp.Width, transparencyPreviews[0].Height - lockHelp.Height);
            lockHelp.PaintSurround = g => transparencyPreviews[0].DrawPreview(g, new RectangleF(-lockHelp.Left, -lockHelp.Top, transparencyPreviews[0].Width, transparencyPreviews[0].Height));
            lockHelp.BringToFront();
            Action saveTransparency = () =>
            {
                EndGlassPreview(null, EventArgs.Empty);
                Theme.EndAppearancePreview(this);
                if (appearance != Theme.Appearance)
                    Theme.SaveAppearance(appearance);
                if (lockGlass.Checked != TransparencyPreferences.Locked || childGlass.Checked != TransparencyPreferences.Children || mainGlass.Checked != TransparencyPreferences.Main)
                    TransparencyPreferences.Save(lockGlass.Checked, childGlass.Checked, mainGlass.Checked);
            };
            foreach (Control control in Controls)
                if (control.Top >= 346 && control != themeChoices)
                    control.Top += 20;
            status.SetBounds(24, 482, settingsHost == null ? 160 : 270, 34);
            cancel.Top = save.Top = 482;
            ClientSize = new Size(440, 530);
            AutoScroll = false;

            cancel.TabIndex = 13;
            save.TabIndex = 14;
            save.Click += (s, e) =>
            {
                float oldScale = DisplaySettings.FontScale, oldWindowScale = DisplaySettings.WindowScale;
                bool oldBadges = DisplaySettings.ShowBadges, kept = false;
                if (Math.Abs(scale - oldScale) < 0.001 && badges == oldBadges && Math.Abs(windowScale - oldWindowScale) < .001)
                {
                    try
                    {
                        saveTransparency();
                        if (settingsHost == null)
                        {
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                        else
                        {
                            status.ForeColor = Theme.AccentForeground;
                            status.Text = "Display settings saved.";
                        }
                    }
                    catch (System.IO.IOException) { status.Text = "Unable to save transparency preferences."; }
                    catch (UnauthorizedAccessException) { status.Text = "Unable to save transparency preferences."; }
                    return;
                }
                try
                {
                    DisplaySettings.ApplyDisplay(scale, badges, windowScale);
                    var hidden = new System.Collections.Generic.List<Form>();
                    Form origin = settingsHost ?? this;
                    while (origin is DisplayDialog || origin is SettingsDialog)
                    {
                        hidden.Add(origin);
                        if (origin.Owner == null)
                            break;
                        origin = origin.Owner;
                    }
                    DialogResult decision;
                    try
                    {
                        foreach (var window in hidden)
                            window.Hide();
                        using (var confirmation = new DisplayConfirmationDialog())
                        {
                            if (!hidden.Contains(origin))
                            {
                                confirmation.StartPosition = FormStartPosition.Manual;
                                confirmation.Location = ConfirmationLocation(origin.Bounds, confirmation.Size, Screen.FromControl(origin).WorkingArea);
                            }
                            decision = confirmation.ShowDialog(hidden.Contains(origin) ? null : origin);
                        }
                    }
                    finally
                    {
                        for (int i = hidden.Count - 1; i >= 0; i--)
                            if (!hidden[i].IsDisposed)
                                hidden[i].Show();
                    }
                    if (decision != DialogResult.OK)
                        return;
                    DisplaySettings.SaveDisplay(scale, badges, windowScale);
                    saveTransparency();
                    kept = true;
                    if (settingsHost == null)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        status.ForeColor = Theme.AccentForeground;
                        status.Text = "Display settings saved.";
                    }
                }
                catch (System.IO.IOException) { status.Text = "Unable to save display preferences."; }
                catch (UnauthorizedAccessException) { status.Text = "Unable to save display preferences."; }
                finally { if (!kept) DisplaySettings.ApplyDisplay(oldScale, oldBadges, oldWindowScale); }
            };
            if (settingsHost != null)
            {
                TopLevel = false;
                FormBorderStyle = FormBorderStyle.None;
                foreach (Control control in Controls)
                    control.Top -= 38;
                cancel.Visible = false;
                save.Text = "Apply";
                CancelButton = null;
                AcceptButton = null;
                ClientSize = new Size(440, 492);

            }
        }
        internal static Point ConfirmationLocation(Rectangle origin, Size dialog, Rectangle work)
        {
            int x = origin.Left + (origin.Width - dialog.Width) / 2, y = origin.Bottom + 12;
            if (y + dialog.Height > work.Bottom)
            {
                if (origin.Top - dialog.Height - 12 >= work.Top)
                    y = origin.Top - dialog.Height - 12;
                else if (origin.Right + 12 + dialog.Width <= work.Right)
                {
                    x = origin.Right + 12;
                    y = origin.Top;
                }
                else if (origin.Left - dialog.Width - 12 >= work.Left)
                {
                    x = origin.Left - dialog.Width - 12;
                    y = origin.Top;
                }
                else
                {
                    x = origin.Right - dialog.Width;
                    y = origin.Bottom - dialog.Height;
                }
            }
            return new Point(Math.Max(work.Left, Math.Min(x, work.Right - dialog.Width)), Math.Max(work.Top, Math.Min(y, work.Bottom - dialog.Height)));
        }
    }
}
