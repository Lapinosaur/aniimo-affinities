using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed partial class MainForm
    {
        private readonly Dictionary<string, Form> contentWindows = new Dictionary<string, Form>();
        private bool contentEvents;
        private void RefreshContentOwnership()
        {
            foreach (var window in contentWindows.Values.ToArray())
                if (!window.IsDisposed)
                    window.Owner = ContentWindowPreferences.KeepVisible ? null : this;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var window in contentWindows.Values.ToArray())
            {
                if (!CloseChildren(window))
                {
                    e.Cancel = true;
                    continue;
                }
                window.Close();
                if (!window.IsDisposed && window.Visible)
                    e.Cancel = true;
            }
            base.OnFormClosing(e);
        }
        private Form OpenContent(string key, Func<Form> create)
        {
            if (!contentEvents)
            {
                contentEvents = true;
                ContentWindowPreferences.Changed += RefreshContentOwnership;
                Disposed += (s, e) =>
                {
                    ContentWindowPreferences.Changed -= RefreshContentOwnership;
                    foreach (var remaining in contentWindows.Values.ToArray())
                        remaining.Dispose();
                    contentWindows.Clear();
                };
            }
            Form window;
            if (contentWindows.TryGetValue(key, out window) && !window.IsDisposed)
            {
                if (window.WindowState == FormWindowState.Minimized)
                    window.WindowState = FormWindowState.Normal;
                window.Activate();
                return window;
            }
            window = create();
            window.ShowInTaskbar = true;
            window.MinimizeBox = true;
            contentWindows[key] = window;
            window.FormClosed += (s, e) => contentWindows.Remove(key);
            window.Show(this);
            RefreshContentOwnership();
            return window;
        }
    }
}
