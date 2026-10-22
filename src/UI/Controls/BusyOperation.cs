using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class BusyOperation
    {
        internal static void OpenLink(Control owner, SettingsButton button, string url, Action<Exception> completed)
        {
            Run(owner, button, "Opening…", () =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }, (result, error) => completed(error));
        }
        // Work never touches controls. Completion and state restoration run on the UI thread.
        internal static bool Run<T>(Control owner, SettingsButton button, string label, Func<T> work, Action<T, Exception> completed, bool keepBusy = false)
        {
            if (owner.IsDisposed || !button.BeginBusy(label))
                return false;
            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) => e.Result = work();
            worker.RunWorkerCompleted += (s, e) =>
            {
                worker.Dispose();
                if (!keepBusy || e.Error != null)
                    button.EndBusy();
                if (owner.IsDisposed || owner.Disposing || button.IsDisposed)
                    return;
                completed(e.Error == null ? (T)e.Result : default(T), e.Error);
            };
            try
            {
                worker.RunWorkerAsync();
            }
            catch
            {
                worker.Dispose();
                button.EndBusy();
                throw;
            }
            return true;
        }
    }
}
