using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class TutorialOverlay
    {
        private readonly Timer pinAnimationTimer = new Timer { Interval = 50 };
        private readonly Stopwatch pinAnimationClock = new Stopwatch();
        private MainForm pinAnimationHost;
        private string pinAnimationView, pinAnimationState;
        private bool pinGesture;

        private void StartPinAnimation(string view)
        {
            if (view != "pin-pinned" && view != "pin-hold")
                return;
            pinAnimationView = view;
            pinAnimationHost = source.CreateTutorialPreview();
            pinAnimationHost.Opacity = 0;
            pinAnimationHost.ShowInTaskbar = false;
            pinAnimationHost.StartPosition = FormStartPosition.Manual;
            pinAnimationHost.Location = Location;
            pinAnimationHost.Show();
            pinAnimationClock.Restart();
            pinAnimationTimer.Tick += AnimatePin;
            AnimatePin(null, EventArgs.Empty);
            pinAnimationTimer.Start();
        }

        internal static string PinAnimationState(bool hold, double milliseconds, out float progress, out bool gesture)
        {
            double time = milliseconds % 4000;
            progress = hold ? (float)Math.Max(0, Math.Min(1, (time - 600) / 1200)) : 0;
            gesture = time >= 600 && time < (hold ? 1800 : 850);
            return hold ? (time >= 1800 ? "pin-locked" : "pin-pinned") : (time >= 850 ? "pin-pinned" : "pin-normal");
        }

        private void AnimatePin(object sender, EventArgs e)
        {
            if (pinAnimationHost == null || !Visible)
                return;
            float progress;
            string state = PinAnimationState(pinAnimationView == "pin-hold", pinAnimationClock.Elapsed.TotalMilliseconds, out progress, out pinGesture);
            bool changed = state != pinAnimationState;
            if (changed)
            {
                pinAnimationHost.SetTutorialPinState(state);
                pinAnimationState = state;
            }
            if (!changed && !pinGesture)
                return;
            pinAnimationHost.SetTutorialHoldProgress(progress);
            var frame = frames[pinAnimationView];
            if (frame.Image.Size != pinAnimationHost.Size)
            {
                frame.Image.Dispose();
                frame.Image = new Bitmap(pinAnimationHost.Width, pinAnimationHost.Height);
            }
            using (var graphics = Graphics.FromImage(frame.Image))
                graphics.Clear(Theme.Background);
            pinAnimationHost.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, frame.Image.Size));
            frame.Targets["pin"] = pinAnimationHost.DrawTutorialPinState(frame.Image);
            ArrangeStep();
            Invalidate();
        }

        private void PaintPinGesture(Graphics graphics)
        {
            if (!pinGesture)
                return;
            float radius = 5 + (float)(pinAnimationClock.Elapsed.TotalMilliseconds % 350) / 70;
            using (var pen = new Pen(Theme.AccentForeground, 2))
                graphics.DrawEllipse(pen, spotlight.Left + spotlight.Width / 2f - radius, spotlight.Bottom - 12 - radius, radius * 2, radius * 2);
        }

        private void StopPinAnimation()
        {
            pinAnimationTimer.Stop();
            pinAnimationTimer.Tick -= AnimatePin;
            pinAnimationClock.Reset();
            pinGesture = false;
            pinAnimationState = null;
            if (pinAnimationHost != null)
            {
                pinAnimationHost.Dispose();
                pinAnimationHost = null;
            }
        }
    }
}
