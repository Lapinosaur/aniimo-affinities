using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class SettingsDialog
    {
        private void AddTechnicalHelp(Panel panel)
        {
            panel.Height = 390;
            panel.Controls.Add(UiFonts.Own(new Label { Text = "Need a hand?", Location = new Point(0, 174), Size = new Size(412, 27), Font = UiFonts.Create("Segoe UI", 13, FontStyle.Bold), ForeColor = Theme.TextColor }));
            panel.Controls.Add(new Label { Text = "For technical help, bugs or questions, join our Discord.\nThe Member role opens #support and #feedback.", Location = new Point(0, 207), Size = new Size(412, 40), ForeColor = Theme.Muted });
            var feedback = new Label { Location = new Point(0, 355), Size = new Size(412, 34), ForeColor = Theme.Muted };
            var discord = new SettingsButton { Text = "Get help on Discord", Glyph = "discord", GlyphWithText = true, Brand = Color.FromArgb(88, 101, 242), Location = new Point(0, 251), Size = new Size(412, 36) };
            discord.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://discord.gg/B6n83stRcf") { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception) { feedback.Text = "Unable to open Discord in your browser."; }
                catch (InvalidOperationException) { feedback.Text = "Unable to open Discord in your browser."; }
            };
            panel.Controls.Add(discord);
            tips.SetToolTip(discord, "Permanent invite · Member role included");
            var copy = new SettingsButton { Text = "Copy diagnostics", Glyph = "copy", GlyphWithText = true, Location = new Point(0, 302), Size = new Size(202, 34) };
            copy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(DiagnosticReport.Create());
                    copy.Text = "Copied";
                    copy.Glyph = "check";
                    feedback.Text = "Paste the diagnostics in #support when asking for help.";
                }
                catch (System.Runtime.InteropServices.ExternalException) { copy.Text = "Try again"; copy.Glyph = "copy"; feedback.Text = "Clipboard unavailable. Please try again."; }
                copy.Invalidate();
            };
            tips.SetToolTip(copy, "Copy technical information. Nothing is sent automatically.");
            panel.Controls.Add(copy);
            var storage = new SettingsButton { Text = "Local storage", Glyph = "folder", GlyphWithText = true, Location = new Point(210, 302), Size = new Size(202, 34) };
            storage.Click += (s, e) => { using (var window = new StorageWindow()) window.ShowDialog(this); };
            tips.SetToolTip(storage, "Open the data folder or clear temporary files");
            panel.Controls.Add(storage);
            panel.Controls.Add(feedback);
        }
    }
}
