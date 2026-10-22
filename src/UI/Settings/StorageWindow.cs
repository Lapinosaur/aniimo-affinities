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
            Controls.Add(summary);
            Controls.Add(new Label { Text = "Remove temporary update files and the last error report.\nYour settings, shortcuts and tutorial preference are kept.\nA prepared update and the previous EXE backup are kept.", Location = new Point(24, 94), Size = new Size(392, 66), ForeColor = Theme.Muted });
            var status = new Label { Location = new Point(24, 164), Size = new Size(392, 34) };
            Controls.Add(status);
            var open = new SettingsButton { Text = "Open folder", Glyph = "folder", GlyphWithText = true, Location = new Point(24, 210), Size = new Size(160, 34) };
            open.Click += (s, e) => BusyOperation.Run(this, open, "Opening…", () =>
            {
                Directory.CreateDirectory(PreferenceFiles.Root);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(PreferenceFiles.Root) { UseShellExecute = true });
                return true;
            }, (result, error) =>
            {
                status.Text = error == null ? "Data folder opened." : "Unable to open the data folder. Please try again.";
                if (error != null)
                    DiagnosticReport.Record(error);
            });
            Controls.Add(open);
            var clear = new SettingsButton { Text = "Clear cache", Glyph = "trash", GlyphWithText = true, Primary = true, Location = new Point(256, 210), Size = new Size(160, 34) };
            Action<bool> measure = cleanup =>
            {
                status.Text = cleanup ? "Removing temporary files…" : "Calculating local storage…";
                BusyOperation.Run(this, clear, cleanup ? "Cleaning…" : "Measuring…", () =>
                {
                    long removed = cleanup ? ReleaseUpdates.ClearCache() : 0;
                    long total = 0;
                    if (Directory.Exists(PreferenceFiles.Root))
                        foreach (string file in Directory.GetFiles(PreferenceFiles.Root, "*", SearchOption.AllDirectories))
                            total += new FileInfo(file).Length;
                    return new[] { removed, total };
                }, (result, error) =>
                {
                    if (error != null)
                    {
                        DiagnosticReport.Record(error);
                        summary.Text = "App data: unavailable";
                        status.Text = error is UnauthorizedAccessException ? "Access denied. No settings were removed." : "Storage unavailable. Close and try again.";
                    }
                    else
                    {
                        summary.Text = "App data: " + (result[1] / 1024.0).ToString("0.0") + " KB";
                        status.Text = cleanup ? "Cleared " + (result[0] / 1024.0).ToString("0.0") + " KB. Files in use are kept." : "";
                    }
                });
            };
            clear.Click += (s, e) => measure(true);
            Controls.Add(clear);
            Shown += (s, e) => measure(false);
        }
    }
}
