using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class UpdatePrompt
    {
        private static bool busy;
        private static bool manualRequested;
        private static Form resultOwner;
        private static readonly List<SettingsButton> buttons = new List<SettingsButton>();
        internal static void Track(SettingsButton button)
        {
            buttons.Add(button);
            button.Disposed += (s, e) => buttons.Remove(button);
            if (busy)
                button.BeginBusy("Checking…");
        }
        internal static void OnStartup(Form owner)
        {
            var timer = new Timer { Interval = 1000 };
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
            if (owner == null || owner.IsDisposed)
                return;
            if (manual)
            {
                manualRequested = true;
                resultOwner = owner;
            }
            if (busy)
                return;
            if (!manual)
                resultOwner = owner;
            busy = true;
            foreach (var button in buttons)
                button.BeginBusy("Checking…");
            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) => e.Result = ReleaseUpdates.Check();
            worker.RunWorkerCompleted += (s, e) =>
            {
                busy = false;
                worker.Dispose();
                foreach (var button in buttons)
                    button.EndBusy();
                manual = manualRequested;
                manualRequested = false;
                owner = resultOwner;
                resultOwner = null;
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
