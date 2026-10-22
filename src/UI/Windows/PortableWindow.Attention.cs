using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal partial class PortableWindow
    {
        private readonly Timer attentionTimer = new Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch attentionWatch = new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch attentionSoundWatch = new System.Diagnostics.Stopwatch();
        private Point attentionOrigin;
        private PortableWindow BlockingDialog()
        {
            foreach (var owned in OwnedForms)
            {
                var dialog = owned as PortableWindow;
                if (dialog != null && dialog.Visible && dialog.Modal)
                    return dialog.BlockingDialog() ?? dialog;
            }
            return null;
        }
        private bool RequestAttention()
        {
            if (attentionTimer.Enabled)
                return false;
            Activate();
            attentionOrigin = Location;
            attentionWatch.Restart();
            attentionTimer.Start();
            if (attentionSoundWatch.IsRunning && attentionSoundWatch.ElapsedMilliseconds < 800)
                return false;
            attentionSoundWatch.Restart();
            return true;
        }
        internal static Point AttentionOffset(double progress)
        {
            if (progress <= 0 || progress >= 1)
                return Point.Empty;
            double decay = (1 - progress) * (1 - progress);
            return new Point((int)Math.Round(Math.Sin(progress * Math.PI * 8) * 8 * decay), (int)Math.Round(Math.Sin(progress * Math.PI * 10) * 5 * decay));
        }
        private void StopAttention()
        {
            if (!attentionTimer.Enabled)
                return;
            attentionTimer.Stop();
            attentionWatch.Reset();
            if (!IsDisposed)
                Location = attentionOrigin;
        }
    }
}
