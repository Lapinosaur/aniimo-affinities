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
        private bool badges = DisplaySettings.ShowBadges;
        private readonly Form settingsHost;
        private Panel zoomChoices;
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
                DisplaySettings.DisplayChanged -= SyncZoom;
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
        internal DisplayDialog(Form host = null, Func<int> mainType = null, Func<Keys[]> previewBindings = null)
        {
            settingsHost = host;
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
            Controls.Add(new Label { Text = "Text size", Location = new Point(24, 52), Size = new Size(380, 24) });
            for (int i = 0; i < 3; i++)
            {
                float value = 0.9f + i * 0.1f;
                var preview = new DisplayChoice
                {
                    Text = new[] { "Small · 90%", "Default · 100%", "Large · 110%" }[i],
                    PreviewScale = value,
                    Location = new Point(24 + i * 132, 80),
                    Size = new Size(128, 100),
                    TabIndex = i,
                    Checked = Math.Abs(scale - value) < .01,
                    AccessibleName = "Text size: " + Math.Round(value * 100) + " percent"
                };
                preview.CheckedChanged += (s, e) => { if (preview.Checked) scale = value; };
                Controls.Add(preview);
            }
            Controls.Add(new Label { Text = "Keyboard badges", Location = new Point(24, 326), Size = new Size(200, 26) });
            var badgeChoices = new Panel { Location = new Point(224, 320), Size = new Size(192, 72), TabIndex = 8, AccessibleName = "Keyboard badge visibility" };
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
            Controls.Add(new Label { Text = "Main / Sub labels always remain visible.", ForeColor = Theme.Muted, Location = new Point(24, 354), Size = new Size(392, 28) });
            var status = new Label { ForeColor = Theme.Error, Location = new Point(24, 376), Size = new Size(392, 22) };
            Controls.Add(status);
            var cancel = new SettingsButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(196, 400), Size = new Size(106, 34), TabIndex = 4 };
            var save = new SettingsButton { Text = "Save", Primary = true, Location = new Point(310, 400), Size = new Size(106, 34), TabIndex = 5 };
            Controls.Add(cancel);
            Controls.Add(save);
            CancelButton = cancel;
            AcceptButton = save;
            Controls.Add(new Label { Text = "Window scale", Location = new Point(24, 216), Size = new Size(392, 22) });
            var windowChoices = new Panel { Location = new Point(24, 242), Size = new Size(392, 72), TabIndex = 3, AccessibleName = "Window size" };
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
            foreach (Control control in Controls)
            {
                if (control.Top >= 354)
                    control.Top += 40;
                if (control.Top >= 216)
                    control.Top -= 26;
            }
            cancel.TabIndex = 10;
            save.TabIndex = 11;
            save.Click += (s, e) =>
            {
                float oldScale = DisplaySettings.FontScale, oldWindowScale = DisplaySettings.WindowScale;
                bool oldBadges = DisplaySettings.ShowBadges, kept = false;
                if (Math.Abs(scale - oldScale) < 0.001 && badges == oldBadges && Math.Abs(windowScale - oldWindowScale) < .001)
                {
                    if (settingsHost == null)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
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
                ClientSize = new Size(440, 422);
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
