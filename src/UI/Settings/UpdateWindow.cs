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
        private bool downloading, ready, restarting;
        internal UpdateWindow(ReleaseUpdate update, Func<string, bool> install = null, Action exit = null)
        {
            Text = "Update available";
            ClientSize = new Size(560, 580);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Background;
            ForeColor = Theme.TextColor;
            UiFonts.Assign(this, UiFonts.Create("Segoe UI", 9));
            Controls.Add(UiFonts.Own(new Label { Text = "Version " + update.Version + " is available", Location = new Point(24, 54), Size = new Size(512, 30), Font = UiFonts.Create("Segoe UI", 13, FontStyle.Bold) }));
            var status = new Label { Text = "Download now, then restart to install the update.", Location = new Point(24, 92), Size = new Size(382, 44) };
            Controls.Add(status);
            status.Width = 512;
            Controls.Add(UpdateHistoryView.Create(update));
            int percent = 0;
            var progress = new DrawingPanel { Location = new Point(24, 143), Size = new Size(382, 8), AccessibleRole = AccessibleRole.ProgressBar, AccessibleName = "Download progress" };
            progress.Paint += (s, e) => { e.Graphics.Clear(Theme.Border); using (var brush = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(brush, 0, 0, progress.Width * percent / 100f, progress.Height); };
            Controls.Add(progress);
            var skip = new SettingsButton { Text = "Skip this version", Location = new Point(24, 182), Size = new Size(114, 34) };
            var later = new SettingsButton { Text = "Remind me later", Location = new Point(146, 182), Size = new Size(114, 34) };
            var download = new SettingsButton { Text = "Download", Primary = true, Location = new Point(268, 182), Size = new Size(138, 34) };
            Controls.Add(skip);
            Controls.Add(later);
            Controls.Add(download);
            progress.SetBounds(24, 494, 512, 8);
            progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            skip.SetBounds(24, 522, 136, 34);
            later.SetBounds(168, 522, 136, 34);
            download.SetBounds(312, 522, 224, 34);
            foreach (var button in new[] { skip, later, download })
                button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
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
                    restarting = true;
                    later.Enabled = false;
                    status.Text = "Preparing the restart… Please wait.";
                    string executable = Application.ExecutablePath;
                    BusyOperation.Run(this, download, "Restarting…", () =>
                    {
                        if (!(install ?? ReleaseUpdates.BeginInstall)(executable))
                            throw new System.IO.IOException("No prepared update is available.");
                        return true;
                    }, (result, error) =>
                    {
                        restarting = false;
                        if (error == null)
                            (exit ?? Application.Exit)();
                        else
                        {
                            DiagnosticReport.Record(error);
                            later.Enabled = true;
                            status.Text = "Unable to restart. Your current app is still open. Try again.";
                        }
                    }, true);
                    return;
                }
                downloading = true;
                download.BeginBusy("Downloading…");
                skip.Enabled = false;
                later.Text = "Cancel";
                status.Text = "Downloading… 0%";
                string target = Application.ExecutablePath;
                var worker = new BackgroundWorker { WorkerReportsProgress = true };
                worker.DoWork += (a, b) => ReleaseUpdates.Prepare(update, target, value => worker.ReportProgress(value), cancellation.Token);
                worker.ProgressChanged += (a, b) => { if (!IsDisposed) { percent = b.ProgressPercentage; status.Text = percent >= 100 ? "Verifying and preparing the update…" : "Downloading… " + percent + "%"; progress.Invalidate(); } };
                worker.RunWorkerCompleted += (a, b) =>
                {
                    downloading = false;
                    worker.Dispose();
                    cancellation.Dispose();
                    if (IsDisposed)
                        return;
                    download.EndBusy();
                    if (b.Error != null)
                    {
                        DiagnosticReport.Record(b.Error);
                        status.Text = "Download failed. Your current app is unchanged. Close and try again.";
                        download.Enabled = false;
                    }
                    else
                    {
                        percent = 100;
                        progress.Invalidate();
                        ready = true;
                        skip.Visible = false;
                        download.Text = "Restart app";
                        download.Enabled = true;
                        status.Text = "Update ready. Restart Affinisaur to apply it. Your PC stays on.";
                    }
                    later.Text = ready ? "Later" : "Close";
                };
                worker.RunWorkerAsync();
            };
            FormClosing += (s, e) => { if (restarting) e.Cancel = true; else if (downloading) cancellation.Cancel(); };
            Disposed += (s, e) => { if (!downloading) cancellation.Dispose(); };
        }
    }
}
