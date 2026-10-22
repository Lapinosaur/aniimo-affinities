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
            foreach (Form form in Application.OpenForms)
                if (form is UpdateWindow)
                {
                    if (manual)
                        form.Activate();
                    return;
                }
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
                    DiagnosticReport.Record(e.Error);
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
                if (!manual)
                {
                    try
                    {
                        if (System.IO.File.ReadAllText(PreferenceFiles.FilePath("skipped-update.txt")).Trim() == update.Version)
                            return;
                    }
                    catch (System.IO.IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                new UpdateWindow(update).Show(owner);
            };
            worker.RunWorkerAsync();
        }
    }
}
