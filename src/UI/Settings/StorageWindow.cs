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
            Text = Language.T("Local storage");
            ClientSize = new Size(440, 378);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            var summary = new Label { Location = new Point(24, 56), Size = new Size(392, 28) };
            Controls.Add(summary);
            Controls.Add(new Label { Text = Language.T("Remove temporary update files and the last error report.\nYour settings, shortcuts and tutorial preference are kept.\nA prepared update and the previous EXE backup are kept."), Location = new Point(24, 94), Size = new Size(392, 66), ForeColor = Theme.Muted });
            var status = new Label { Location = new Point(24, 164), Size = new Size(392, 34) };
            Controls.Add(status);
            var open = new SettingsButton { Text = Language.T("Open folder"), Glyph = "folder", GlyphWithText = true, Location = new Point(24, 210), Size = new Size(160, 34) };
            open.Click += (s, e) => BusyOperation.Run(this, open, Language.T("Opening…"), () =>
            {
                Directory.CreateDirectory(PreferenceFiles.Root);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(PreferenceFiles.Root) { UseShellExecute = true });
                return true;
            }, (result, error) =>
            {
                status.Text = error == null ? Language.T("Data folder opened.") : Language.T("Unable to open the data folder. Please try again.");
                if (error != null)
                    DiagnosticReport.Record(error);
            });
            Controls.Add(open);
            var clear = new SettingsButton { Text = Language.T("Clear cache"), Glyph = "trash", GlyphWithText = true, Primary = true, Location = new Point(256, 210), Size = new Size(160, 34) };
            Action<bool> measure = cleanup =>
            {
                status.Text = cleanup ? Language.T("Removing temporary files…") : Language.T("Calculating local storage…");
                BusyOperation.Run(this, clear, cleanup ? Language.T("Cleaning…") : Language.T("Measuring…"), () =>
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
                        summary.Text = Language.T("App data: unavailable");
                        status.Text = error is UnauthorizedAccessException ? Language.T("Access denied. No settings were removed.") : Language.T("Storage unavailable. Close and try again.");
                    }
                    else
                    {
                        summary.Text = Language.T("App data: ") + (result[1] / 1024.0).ToString("0.0") + " KB";
                        status.Text = cleanup ? Language.T("Cleared ") + (result[0] / 1024.0).ToString("0.0") + Language.T(" KB. Files in use are kept.") : "";
                    }
                });
            };
            clear.Click += (s, e) => measure(true);
            Controls.Add(clear);
            Controls.Add(new Label { Text = Language.T("Start over for troubleshooting"), Bounds = new Rectangle(24, 266, 392, 24), ForeColor = Theme.TextColor });
            var reset = new SettingsButton { Text = Language.T("Reset app to defaults…"), Glyph = "reset", GlyphWithText = true, Bounds = new Rectangle(24, 306, 392, 36) };
            reset.Click += (s, e) =>
            {
                if (MessageBox.Show(this, Language.T("Reset the entire app?\n\nYour team, settings, shortcuts, tutorial progress and onboarding will be reset. The app will close and reopen at first launch.\n\nYour current profile will be moved to a local backup beside the data folder. Nothing is sent online."), Language.T("Reset Affinisaur"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.OK)
                    return;
                try
                {
                    FactoryReset.Request();
                    Application.Exit();
                }
                catch (IOException) { status.Text = Language.T("Unable to prepare the reset. Your data is unchanged."); }
                catch (UnauthorizedAccessException) { status.Text = Language.T("Access denied. Your data is unchanged."); }
            };
            Controls.Add(reset);
            Shown += (s, e) => measure(false);
        }
    }
}
