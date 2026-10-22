using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class AboutPanel : Panel
    {
        private readonly ApplicationTips tips = ApplicationTips.Create();
        private readonly Label status;
        internal AboutPanel()
        {
            Size = new Size(412, 514);
            BackColor = Theme.Background;
            AddText("Aniimo Affinities", 0, 28, 16, true, Theme.TextColor);
            AddText("Explore type matchups, matrices and affinity radars.", 34, 24, 9, false, Theme.Muted);
            AddText("CONNECT WITH LAPINOSAUR", 76, 20, 8, true, Theme.Muted);
            AddLink("Twitch", "Live streams", "https://www.twitch.tv/lapinosaur", 0, 102);
            AddLink("GitHub", "Projects & source", "https://github.com/Lapinosaur", 212, 102);
            AddLink("YouTube", "Videos & tutorials", "https://www.youtube.com/@LapinosaurMedia", 0, 170);
            AddLink("Buy Me a Coffee", "Support my work", "https://buymeacoffee.com/lapinosaur", 212, 170);
            AddText("CREDITS", 250, 20, 8, true, Theme.Muted);
            AddText("Design & development: Lapinosaur\nMatchup data: SaveOverride Aniimo models", 276, 44, 9, false, Theme.TextColor);
            var source = new SettingsButton { Text = "Type icons · ManuelGraf / AniimoWeaknessCalculator", Location = new Point(0, 324), Size = new Size(412, 32), AccessibleName = "Open type icon source on GitHub" };
            source.Click += (s, e) => OpenLink("https://github.com/ManuelGraf/AniimoWeaknessCalculator/blob/main/src/aniimo-icons.svg");
            Controls.Add(source);
            tips.SetToolTip(source, "Community traces of game artwork. Redistribution rights are not established; no third-party asset license is implied.");
            AddText("Unofficial fan tool. Not affiliated with or endorsed by\nAniimo or its rights holders.", 366, 44, 8, false, Theme.Muted);
            var updates = new SettingsButton { Text = "Check for updates", Location = new Point(212, 416), Size = new Size(200, 32) };
            updates.Click += (s, e) => UpdatePrompt.Check(FindForm(), true);
            Controls.Add(updates);
            Controls.Add(new Label { Text = "Version " + ReleaseUpdates.CurrentVersion, Location = new Point(0, 422), Size = new Size(200, 22), ForeColor = Theme.Muted });
            updates.BringToFront();
            var diagnostics = new SettingsButton { Text = "Copy diagnostics", Location = new Point(0, 452), Size = new Size(200, 32) };
            diagnostics.Click += (s, e) => { try { Clipboard.SetText(DiagnosticReport.Create()); diagnostics.Text = "Copied"; } catch (System.Runtime.InteropServices.ExternalException) { diagnostics.Text = "Try again"; } };
            Controls.Add(diagnostics);
            var storage = new SettingsButton { Text = "Local storage", Location = new Point(212, 452), Size = new Size(200, 32) };
            storage.Click += (s, e) => { using (var dialog = new StorageWindow()) dialog.ShowDialog(FindForm()); };
            Controls.Add(storage);
            status = new Label { Location = new Point(0, 486), Size = new Size(412, 28), ForeColor = Theme.Error };
            Controls.Add(status);
        }
        private void AddText(string text, int top, int height, float size, bool bold, Color color)
        {
            Controls.Add(UiFonts.Own(new Label { UseMnemonic = false, Text = text, Location = new Point(0, top), Size = new Size(412, height), ForeColor = color, Font = UiFonts.Create("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular) }));
        }
        private void AddLink(string name, string description, string url, int left, int top)
        {
            var button = new SettingsButton { Text = name, GlyphWithText = true, Glyph = name == "Twitch" ? "twitch" : name == "GitHub" ? "github" : name == "YouTube" ? "youtube" : "coffee", Location = new Point(left, top), Size = new Size(200, 34), AccessibleName = name + " · " + description };
            button.Click += (s, e) => OpenLink(url);
            tips.SetToolTip(button, "Open in your browser · " + url);
            Controls.Add(button);
            Controls.Add(UiFonts.Own(new Label { UseMnemonic = false, Text = description, Location = new Point(left, top + 38), Size = new Size(200, 22), ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8), TextAlign = ContentAlignment.MiddleCenter }));
        }
        private void OpenLink(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                status.Text = "";
            }
            catch (System.ComponentModel.Win32Exception) { status.Text = "Unable to open your browser. Please try again."; }
            catch (InvalidOperationException) { status.Text = "Unable to open your browser. Please try again."; }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
