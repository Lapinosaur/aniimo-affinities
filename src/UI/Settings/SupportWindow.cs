using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SupportWindow : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        protected override string CaptionGlyph
        {
            get
            {
                return "help";
            }
        }
        internal static void Open(Form owner)
        {
            var existing = Application.OpenForms.OfType<SupportWindow>().FirstOrDefault();
            if (existing != null)
            {
                if (owner != null && owner != existing)
                {
                    existing.Owner = owner;
                    existing.TopMost = owner.TopMost;
                }
                // A modal dialog may have disabled an already-open Support HWND.
                // Re-enable only Support, leaving the dialog's other windows blocked.
                if (!NativeMethods.IsWindowEnabled(existing.Handle))
                {
                    existing.Enabled = false;
                    existing.Enabled = true;
                }
                if (existing.WindowState == FormWindowState.Minimized)
                    existing.WindowState = FormWindowState.Normal;
                existing.Activate();
                return;
            }
            var window = new SupportWindow();
            window.TopMost = owner != null && owner.TopMost;
            window.Show(owner);
        }
        internal SupportWindow()
        {
            Text = Language.T("Affinisaur · Support");
            ClientSize = new Size(460, 474);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(UiFonts.Own(new Label { Text = Language.T("Let's stay in touch"), Bounds = new Rectangle(24, 56, 412, 36), ForeColor = Theme.TextColor, Font = UiFonts.Create("Segoe UI", 18, FontStyle.Bold) }));
            Controls.Add(new Label { Text = Language.T("Questions, bugs or feedback? Join Lapinosaur on Discord.\nYou can also follow the project and the next creations."), Bounds = new Rectangle(24, 104, 412, 48), ForeColor = Theme.Muted });
            var status = new Label { Bounds = new Rectangle(24, 350, 412, 44), ForeColor = Theme.Muted };
            string[] names = { Language.T("Get help on Discord"), Language.T("Star on GitHub"), "Twitch", "YouTube" };
            string[] glyphs = { "discord", "star", "twitch", "youtube" };
            string[] urls = { "https://discord.gg/B6n83stRcf", "https://github.com/Lapinosaur/aniimo-affinities", "https://www.twitch.tv/lapinosaur", "https://www.youtube.com/@LapinosaurMedia" };
            for (int i = 0; i < names.Length; i++)
            {
                string url = urls[i], name = names[i];
                var button = new SettingsButton { Text = name, Glyph = glyphs[i], GlyphWithText = true, Primary = i == 0, Bounds = new Rectangle(24, 164 + i * 44, 412, 36) };
                ExternalLinkTips.Attach(button, url);
                button.Click += (s, e) => BusyOperation.OpenLink(this, button, url, error =>
                {
                    status.Text = error == null ? name + Language.T(" opened in your browser.") : Language.T("Unable to open the link. Please try again.");
                    if (error != null)
                        DiagnosticReport.Record(error);
                });
                Controls.Add(button);
            }
            Controls.Add(status);
            var reset = new SettingsButton { Text = Language.T("Troubleshooting & reset…"), Glyph = "settings", GlyphWithText = true, Bounds = new Rectangle(24, 396, 412, 32) };
            reset.Click += (s, e) => { using (var storage = new StorageWindow()) storage.ShowDialog(this); };
            Controls.Add(reset);
            Controls.Add(new ShortcutLabel { Text = ShortcutText.Mark("F11") + Language.T(" focuses Support · ") + ShortcutText.Mark("Esc") + Language.T(" closes this window"), Bounds = new Rectangle(24, 440, 412, 24), ForeColor = Theme.Muted });
        }
    }
}
