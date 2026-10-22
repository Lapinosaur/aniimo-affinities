using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class UpdatePrompt
    {
        private static bool busy;
        internal static void OnStartup(Form owner)
        {
            var timer = new Timer { Interval = 10000 };
            owner.Disposed += (s, e) => timer.Dispose();
            timer.Tick += (s, e) =>
            {
                if (Application.OpenForms.Count > 1)
                    return;
                timer.Stop();
                timer.Dispose();
                Check(owner, false);
            };
            timer.Start();
        }
        internal static void Check(Form owner, bool manual)
        {
            if (busy || owner == null || owner.IsDisposed)
                return;
            busy = true;
            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) => e.Result = ReleaseUpdates.Check();
            worker.RunWorkerCompleted += (s, e) =>
            {
                busy = false;
                worker.Dispose();
                if (owner.IsDisposed)
                    return;
                if (e.Error != null)
                {
                    if (manual)
                        MessageBox.Show(owner, "Unable to check for updates. Please try again later.", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var update = (ReleaseUpdate)e.Result;
                if (update == null)
                {
                    if (manual)
                        MessageBox.Show(owner, "You're up to date. Version " + ReleaseUpdates.CurrentVersion + ".", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(owner, "Version " + update.Version + " is available.\n\nDownload now and install the next time you launch the app?", "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Download(owner, update);
            };
            worker.RunWorkerAsync();
        }
        private static void Download(Form owner, ReleaseUpdate update)
        {
            busy = true;
            string target = Application.ExecutablePath;
            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) => ReleaseUpdates.Prepare(update, target);
            worker.RunWorkerCompleted += (s, e) =>
            {
                busy = false;
                worker.Dispose();
                if (owner.IsDisposed)
                    return;
                MessageBox.Show(owner, e.Error == null ? "Update verified and ready. It will install the next time you launch this copy of the app." : "The update could not be downloaded or verified. Your current app is unchanged.", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            worker.RunWorkerAsync();
        }
    }
}
