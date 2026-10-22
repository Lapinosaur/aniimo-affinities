using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class BusyFeedbackTests
    {
        private static void Until(Func<bool> condition)
        {
            var clock = Stopwatch.StartNew();
            while (!condition())
            {
                if (clock.ElapsedMilliseconds > 5000)
                    throw new Exception("Busy operation did not complete.");
                Application.DoEvents();
                Thread.Sleep(5);
            }
        }
        internal static void Run()
        {
            using (var form = new Form())
            using (var button = new SettingsButton { Text = "Original", AccessibleDescription = "Original description" })
            using (var gate = new ManualResetEvent(false))
            using (var started = new ManualResetEvent(false))
            {
                form.Controls.Add(button);
                form.Show();
                int completed = 0, clicks = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                button.Click += (s, e) => clicks++;
                BusyOperation.Run(form, button, "Working…", () => { started.Set(); gate.WaitOne(); return 42; }, (value, error) =>
                {
                    if (value != 42 || error != null || Thread.CurrentThread.ManagedThreadId != uiThread)
                        throw new Exception("Busy completion must return to the UI thread.");
                    completed++;
                });
                try
                {
                    Until(() => started.WaitOne(0));
                    button.PerformClick();
                    if (!button.Busy || button.Enabled || clicks != 0 || button.Text != "Working…")
                        throw new Exception("Busy state must be immediate and prevent duplicate interaction.");
                    if (BusyOperation.Run(form, button, "Duplicate", () => 0, (value, error) => completed++))
                        throw new Exception("A busy action must reject a second operation.");
                    bool ticked = false;
                    using (var timer = new System.Windows.Forms.Timer { Interval = 20 })
                    {
                        timer.Tick += (s, e) => ticked = true;
                        timer.Start();
                        Until(() => ticked);
                    }
                }
                finally { gate.Set(); }
                Until(() => completed == 1);
                if (button.Busy || !button.Enabled || button.Text != "Original" || button.AccessibleDescription != "Original description")
                    throw new Exception("Completion must restore the original action and accessibility text.");
                bool failed = false;
                BusyOperation.Run<int>(form, button, "Failing…", () => { throw new InvalidOperationException("fixture"); }, (value, error) => failed = error is InvalidOperationException);
                Until(() => failed);
                if (button.Busy || !button.Enabled)
                    throw new Exception("Failed actions must be retryable.");
            }
            VerifyRestart(false);
            VerifyRestart(true);
        }
        private static void VerifyRestart(bool succeeds)
        {
            using (var gate = new ManualResetEvent(false))
            using (var started = new ManualResetEvent(false))
            using (var form = new UpdateWindow(new ReleaseUpdate { Version = "test" }, path => { started.Set(); gate.WaitOne(); return succeeds; }, () => { }))
            {
                form.Show();
                var button = form.Controls.OfType<SettingsButton>().Single(x => x.Text == "Download");
                typeof(UpdateWindow).GetField("ready", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, true);
                button.Text = "Restart app";
                button.PerformClick();
                try
                {
                    Until(() => started.WaitOne(0));
                    if (!button.Busy || button.Enabled || button.Text != "Restarting…")
                        throw new Exception("Restart must show a loader before installer preparation finishes.");
                    form.Close();
                    if (form.IsDisposed)
                        throw new Exception("Do not close the update owner halfway through preparing its installer.");
                }
                finally { gate.Set(); }
                var restarting = typeof(UpdateWindow).GetField("restarting", BindingFlags.Instance | BindingFlags.NonPublic);
                Until(() => !(bool)restarting.GetValue(form));
                if (button.Busy != succeeds || button.Enabled == succeeds)
                    throw new Exception("Restart success keeps feedback until exit; failure restores the action.");
            }
        }
    }
}
