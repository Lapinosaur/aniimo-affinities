using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class UpdateWindow : PortableWindow
    {
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool downloading, ready;
        internal UpdateWindow(ReleaseUpdate update)
        {
            Text = "Update available";
            ClientSize = new Size(430, 244);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(UiFonts.Own(new Label { Text = "Version " + update.Version + " is available", Location = new Point(24, 54), Size = new Size(382, 30), Font = UiFonts.Create("Segoe UI", 13, FontStyle.Bold) }));
            var status = new Label { Text = "Download now, then restart to install the update.", Location = new Point(24, 92), Size = new Size(382, 44) };
            Controls.Add(status);
            int percent = 0;
            var progress = new DrawingPanel { Location = new Point(24, 143), Size = new Size(382, 8), AccessibleRole = AccessibleRole.ProgressBar, AccessibleName = "Download progress" };
            progress.Paint += (s, e) => { e.Graphics.Clear(Theme.Border); using (var brush = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(brush, 0, 0, progress.Width * percent / 100f, progress.Height); };
            Controls.Add(progress);
            var skip = new SettingsButton { Text = "Skip this update", Location = new Point(24, 182), Size = new Size(122, 34) };
            var later = new SettingsButton { Text = "Remind me later", Location = new Point(154, 182), Size = new Size(130, 34) };
            var download = new SettingsButton { Text = "Download", Primary = true, Location = new Point(292, 182), Size = new Size(114, 34) };
            Controls.Add(skip);
            Controls.Add(later);
            Controls.Add(download);
            later.Click += (s, e) => Close();
            skip.Click += (s, e) =>
            {
                try
                {
                    PreferenceFiles.WriteText(PreferenceFiles.FilePath("skipped-update.txt"), update.Version);
                    Close();
                }
                catch (Exception error) { DiagnosticReport.Record(error); status.Text = "Unable to save this preference."; }
            };
            download.Click += (s, e) =>
            {
                if (ready)
                {
                    try
                    {
                        if (!ReleaseUpdates.BeginInstall(Application.ExecutablePath))
                            throw new System.IO.IOException("No prepared update is available.");
                        Application.Exit();
                    }
                    catch (Exception error)
                    {
                        DiagnosticReport.Record(error);
                        status.Text = "Unable to restart for the update. Your current app is still open. Try again.";
                    }
                    return;
                }
                downloading = true;
                download.Enabled = skip.Enabled = false;
                later.Text = "Cancel";
                status.Text = "Downloading… 0%";
                string target = Application.ExecutablePath;
                var worker = new BackgroundWorker { WorkerReportsProgress = true };
                worker.DoWork += (a, b) => ReleaseUpdates.Prepare(update, target, value => worker.ReportProgress(value), cancellation.Token);
                worker.ProgressChanged += (a, b) => { if (!IsDisposed) { percent = b.ProgressPercentage; status.Text = "Downloading… " + percent + "%"; progress.Invalidate(); } };
                worker.RunWorkerCompleted += (a, b) =>
                {
                    downloading = false;
                    worker.Dispose();
                    cancellation.Dispose();
                    if (IsDisposed)
                        return;
                    if (b.Error != null)
                    {
                        DiagnosticReport.Record(b.Error);
                        status.Text = "Download failed. Your current app is unchanged. Close and try again.";
                    }
                    else
                    {
                        percent = 100;
                        progress.Invalidate();
                        ready = true;
                        skip.Visible = false;
                        download.Text = "Restart now";
                        download.Enabled = true;
                        status.Text = "Verified and ready. Restart now to install and reopen the app.";
                    }
                    later.Text = ready ? "Restart later" : "Close";
                };
                worker.RunWorkerAsync();
            };
            FormClosing += (s, e) => { if (downloading) cancellation.Cancel(); };
            Disposed += (s, e) => { if (!downloading) cancellation.Dispose(); };
        }
    }
}
