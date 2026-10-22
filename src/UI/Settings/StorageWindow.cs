using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class StorageWindow : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        internal StorageWindow()
        {
            Text = "Local storage";
            ClientSize = new Size(440, 264);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            var summary = new Label { Location = new Point(24, 56), Size = new Size(392, 28) };
            Action refresh = () =>
            {
                long total = 0;
                try
                {
                    if (Directory.Exists(PreferenceFiles.Root))
                        foreach (string file in Directory.GetFiles(PreferenceFiles.Root, "*", SearchOption.AllDirectories))
                            total += new FileInfo(file).Length;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                summary.Text = "App data: " + (total / 1024.0).ToString("0.0") + " KB";
            };
            Controls.Add(summary);
            Controls.Add(new Label { Text = "Remove temporary update files and the last error report.\nYour settings, shortcuts and tutorial preference are kept.\nA prepared update and the previous EXE backup are kept.", Location = new Point(24, 94), Size = new Size(392, 66), ForeColor = Theme.Muted });
            var status = new Label { Location = new Point(24, 164), Size = new Size(392, 34) };
            Controls.Add(status);
            var open = new SettingsButton { Text = "Open folder", Glyph = "folder", GlyphWithText = true, Location = new Point(24, 210), Size = new Size(160, 34) };
            open.Click += (s, e) =>
            {
                try
                {
                    Directory.CreateDirectory(PreferenceFiles.Root);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(PreferenceFiles.Root) { UseShellExecute = true });
                }
                catch (Exception error)
                {
                    if (!(error is IOException || error is UnauthorizedAccessException || error is System.ComponentModel.Win32Exception))
                        throw;
                    status.Text = "Unable to open the data folder.";
                }
            };
            Controls.Add(open);
            var clear = new SettingsButton { Text = "Clear cache", Glyph = "trash", GlyphWithText = true, Primary = true, Location = new Point(256, 210), Size = new Size(160, 34) };
            clear.Click += (s, e) =>
            {
                try
                {
                    long bytes = ReleaseUpdates.ClearCache();
                    status.Text = "Cleared " + (bytes / 1024.0).ToString("0.0") + " KB. Files in use are kept.";
                    refresh();
                }
                catch (IOException) { status.Text = "Cleanup unavailable during an update. Try again later."; }
                catch (UnauthorizedAccessException) { status.Text = "Access denied. No settings were removed."; }
            };
            Controls.Add(clear);
            refresh();
        }
    }
}
