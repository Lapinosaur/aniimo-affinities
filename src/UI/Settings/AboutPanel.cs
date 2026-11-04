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
            AddText("Affinisaur", 0, 28, 16, true, Theme.TextColor);
            AddText(Language.T("Unofficial Aniimo companion by Lapinosaur."), 34, 24, 9, false, Theme.Muted);
            AddText(Language.T("CREDITS"), 250, 20, 8, true, Theme.Muted);
            AddText(Language.T("Design & development: Lapinosaur\nMatchup data: Aniimo type affinities"), 276, 44, 9, false, Theme.TextColor);
            var source = new SettingsButton { Text = Language.T("Type icons · ManuelGraf / AniimoWeaknessCalculator"), Location = new Point(0, 324), Size = new Size(412, 32), AccessibleName = Language.T("Open type icon source on GitHub") };
            source.Click += (s, e) => OpenLink(source, "https://github.com/ManuelGraf/AniimoWeaknessCalculator/blob/main/src/aniimo-icons.svg");
            Controls.Add(source);
            tips.SetToolTip(source, ExternalLinkTips.Describe("https://github.com/ManuelGraf/AniimoWeaknessCalculator/blob/main/src/aniimo-icons.svg") + "\n" + Language.T("Community traces of game artwork. Redistribution rights are not established; no third-party asset license is implied."));
            AddText(Language.T("Unofficial fan tool. Not affiliated with or endorsed by\nAniimo or its rights holders."), 366, 44, 8, false, Theme.Muted);
            var updates = new SettingsButton { Text = Language.T("Check for updates"), Location = new Point(212, 416), Size = new Size(200, 32) };
            UpdatePrompt.Track(updates);
            updates.Click += (s, e) => UpdatePrompt.Check(FindForm(), true);
            Controls.Add(updates);
            Controls.Add(new Label { Tag = 422, Text = Language.T("Version ") + ReleaseUpdates.CurrentVersion, Location = new Point(0, 422), Size = new Size(200, 22), ForeColor = Theme.Muted });
            updates.BringToFront();
            var diagnostics = new SettingsButton { Text = Language.T("Copy diagnostics"), Location = new Point(0, 452), Size = new Size(200, 32) };
            diagnostics.Click += (s, e) => { try { Clipboard.SetText(DiagnosticReport.Create()); diagnostics.Text = Language.T("Copied"); diagnostics.Glyph = "check"; } catch (System.Runtime.InteropServices.ExternalException) { diagnostics.Text = Language.T("Try again"); diagnostics.Glyph = "copy"; } diagnostics.Invalidate(); };
            Controls.Add(diagnostics);
            var storage = new SettingsButton { Text = Language.T("Local storage"), Location = new Point(212, 452), Size = new Size(200, 32) };
            storage.Click += (s, e) => { using (var dialog = new StorageWindow()) dialog.ShowDialog(FindForm()); };
            Controls.Add(storage);
            status = new Label { Location = new Point(0, 486), Size = new Size(412, 28), ForeColor = Theme.Error };
            Controls.Add(status);
            foreach (Control control in Controls)
                if (control.Top >= 250)
                    control.Top += 42;
            Height += 42;
            var discord = new SettingsButton { Text = Language.T("Join the Discord community"), Glyph = "community", GlyphWithText = true, Brand = Color.FromArgb(88, 101, 242), Location = new Point(0, 238), Size = new Size(412, 34), AccessibleName = Language.T("Join the Discord community") };
            discord.Click += (s, e) => OpenLink(discord, "https://discord.gg/B6n83stRcf");
            tips.SetToolTip(discord, ExternalLinkTips.Describe("https://discord.gg/B6n83stRcf"));
            Controls.Add(discord);
            Redesign(discord, source, updates, diagnostics, storage);
        }
        private void Redesign(SettingsButton discord, SettingsButton source, SettingsButton updates, SettingsButton diagnostics, SettingsButton storage)
        {
            Height = 550;
            foreach (Control control in Controls)
            {
                if (control is Label && Equals(control.Tag, 250))
                {
                    control.Text = Language.T("THE PEOPLE & ART BEHIND IT");
                    control.Top = 382;
                }
                else if (control is Label && Equals(control.Tag, 276))
                {
                    control.Text = Language.T("Design & code: Lapinosaur · Data: Aniimo type affinities");
                    control.Top = 403;
                    control.Height = 22;
                    UiFonts.Assign(control, UiFonts.Create("Segoe UI", 8));
                }
                else if (control is Label && Equals(control.Tag, 366))
                {
                    control.Top = 456;
                    control.Height = 32;
                }
                else if (control is Label && Equals(control.Tag, 422))
                {
                    control.Text = "v" + ReleaseUpdates.CurrentVersion;
                    control.SetBounds(2, 498, 55, 22);
                }
            }
            source.SetBounds(0, 424, 412, 28);
            updates.SetBounds(62, 490, 164, 32);
            diagnostics.Text = Language.T("Copy");
            diagnostics.Glyph = "copy";
            diagnostics.GlyphWithText = true;
            tips.SetToolTip(diagnostics, Language.T("Copy diagnostics to the clipboard"));
            diagnostics.AccessibleName = Language.T("Copy diagnostics");
            diagnostics.SetBounds(230, 490, 100, 32);
            storage.Text = "";
            storage.Glyph = "folder";
            tips.SetToolTip(storage, Language.T("Local storage · Open folder or clear cache"));
            storage.AccessibleName = Language.T("Local storage");
            storage.SetBounds(334, 490, 78, 32);
            Controls.Remove(diagnostics);
            Controls.Remove(storage);
            diagnostics.Dispose();
            storage.Dispose();
            updates.SetBounds(212, 490, 200, 32);
            status.Top = 563;
            Height = 590;
            var legal = new SettingsButton { Text = Language.T("Legal & Privacy · CGU"), Glyph = "info", GlyphWithText = true, Location = new Point(0, 527), Size = new Size(412, 32) };
            legal.Click += (s, e) => { using (var window = new LegalWindow()) window.ShowDialog(FindForm()); };
            Controls.Add(legal);
            var community = Card(68, 99, true);
            CardText(community, Language.T("Better together"), 16, 10, 380, 25, 12, true);
            CardText(community, Language.T("Share ideas, get help & follow new releases."), 16, 38, 380, 22, 8.5f, false);
            Controls.Remove(discord);
            community.Controls.Add(discord);
            discord.Text = Language.T("Join Discord");
            discord.Brand = Color.FromArgb(88, 101, 242);
            discord.Glyph = "discord";
            discord.SurroundColor = () => Theme.AccentSurface;
            discord.Primary = false;
            discord.SetBounds(14, 62, 382, 31);
            var support = Card(179, 147, false);
            CardText(support, Language.T("Made with care"), 16, 13, 250, 25, 12, true);
            CardText(support, Language.T("By Lapinosaur, for the community.\nYour support helps me keep creating."), 16, 43, 260, 38, 8.5f, false);
            var carrot = new SupportButton { Location = new Point(13, 84), Size = new Size(166, 40), SurroundColor = () => Theme.Surface };
            tips.SetToolTip(carrot, ExternalLinkTips.Describe("https://buymeacoffee.com/lapinosaur"));
            carrot.Click += (s, e) => OpenLink(carrot, "https://buymeacoffee.com/lapinosaur");
            support.Controls.Add(carrot);
            CardText(support, Language.T("Always optional. Thank you!"), 17, 121, 260, 20, 8, false);
            var qr = new SupportQrButton { Location = new Point(281, 7), Size = new Size(117, 117), Code = carrot.QrCode };
            tips.SetToolTip(qr, ExternalLinkTips.Describe("https://buymeacoffee.com/lapinosaur"));
            qr.Click += (s, e) => OpenLink(carrot, "https://buymeacoffee.com/lapinosaur");
            support.Controls.Add(qr);
            CardText(support, Language.T("Scan or click to support"), 270, 124, 138, 18, 7.5f, false);
            string[] names = { "GitHub", "Twitch", "YouTube" };
            string[] links = { "https://github.com/Lapinosaur/aniimo-affinities", "https://www.twitch.tv/lapinosaur", "https://www.youtube.com/@LapinosaurMedia" };
            for (int i = 0; i < 3; i++)
            {
                string url = links[i];
                var link = new SettingsButton { Text = names[i], Glyph = names[i].ToLowerInvariant(), GlyphWithText = true, Location = new Point(i * 140, 338), Size = new Size(132, 34) };
                link.Brand = i == 1 ? Color.FromArgb(145, 70, 255) : i == 2 ? Color.FromArgb(255, 0, 51) : Color.FromArgb(110, 118, 129);
                link.Click += (s, e) => OpenLink(link, url);
                tips.SetToolTip(link, ExternalLinkTips.Describe(url));
                Controls.Add(link);
            }
        }
        private DrawingPanel Card(int top, int height, bool accent)
        {
            var card = new DrawingPanel { Location = new Point(0, top), Size = new Size(412, height) };
            card.Paint += (s, e) => Drawing.Box(e.Graphics, new RectangleF(0, 0, card.Width - 1, card.Height - 1), accent ? Theme.AccentSurface : Theme.Surface, Theme.Border, 12);
            Controls.Add(card);
            return card;
        }
        private static void CardText(Control parent, string text, int x, int y, int width, int height, float size, bool bold)
        {
            parent.Controls.Add(UiFonts.Own(new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height), BackColor = Color.Transparent, ForeColor = bold ? Theme.TextColor : Theme.Muted, Font = UiFonts.Create("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular) }));
        }
        private void AddText(string text, int top, int height, float size, bool bold, Color color)
        {
            Controls.Add(UiFonts.Own(new Label { UseMnemonic = false, Tag = top, Text = text, Location = new Point(0, top), Size = new Size(412, height), ForeColor = color, Font = UiFonts.Create("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular) }));
        }
        private void AddLink(string name, string description, string url, int left, int top)
        {
            var button = new SettingsButton { Text = name, Brand = name == "Twitch" ? Color.FromArgb(145, 70, 255) : name == "YouTube" ? Color.FromArgb(255, 0, 51) : name == Language.T("Buy Me a Coffee") ? Color.FromArgb(255, 221, 0) : Color.FromArgb(110, 118, 129), GlyphWithText = true, Glyph = name == "Twitch" ? "twitch" : name == "GitHub" ? "github" : name == "YouTube" ? "youtube" : "coffee", Location = new Point(left, top), Size = new Size(200, 34), AccessibleName = name + " · " + description };
            button.Click += (s, e) => OpenLink(button, url);
            tips.SetToolTip(button, Language.T("Open in your browser · ") + url);
            Controls.Add(button);
            Controls.Add(UiFonts.Own(new Label { UseMnemonic = false, Text = description, Location = new Point(left, top + 38), Size = new Size(200, 22), ForeColor = Theme.Muted, Font = UiFonts.Create("Segoe UI", 8), TextAlign = ContentAlignment.MiddleCenter }));
        }
        private void OpenLink(SettingsButton button, string url)
        {
            status.Text = Language.T("Opening your browser…");
            BusyOperation.OpenLink(this, button, url, error =>
            {
                status.Text = error == null ? "" : Language.T("Unable to open your browser. Please try again.");
                if (error != null)
                    DiagnosticReport.Record(error);
            });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
