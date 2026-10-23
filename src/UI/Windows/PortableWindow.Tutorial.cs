using System;
using System.Drawing;

namespace AniimoAffinities
{
    internal partial class PortableWindow
    {
        internal bool TutorialSnapshot;
        protected override bool ShowWithoutActivation
        {
            get
            {
                return TutorialSnapshot || base.ShowWithoutActivation;
            }
        }
        internal void SetTutorialPinState(string view)
        {
            if (!TutorialSnapshot)
                throw new InvalidOperationException("Only isolated tutorial windows may preview pin states.");
            if (OverlayLocked && view != "pin-locked")
                SetOverlayLocked(false);
            SetPinned(view != "pin-normal");
            holdProgress = 0;
            if (view == "pin-locked")
                SetOverlayLocked(true);
            if (view == "pin-hold")
                holdProgress = .72f;
            PerformLayout();
        }

        internal void SetTutorialHoldProgress(float value)
        {
            if (!TutorialSnapshot)
                throw new InvalidOperationException("Only tutorial snapshots may simulate a hold.");
            holdProgress = Math.Max(0, Math.Min(1, value));
        }

        internal Rectangle DrawTutorialPinState(Bitmap image)
        {
            var target = RectangleToClient(pinControl.RectangleToScreen(pinControl.ClientRectangle));
            if (TopMost)
                image.MakeTransparent(Theme.Background);
            if (unlockWindow != null)
            {
                using (var controls = new Bitmap(unlockWindow.Width, unlockWindow.Height))
                using (var graphics = Graphics.FromImage(image))
                {
                    unlockWindow.DrawToBitmap(controls, new Rectangle(Point.Empty, controls.Size));
                    controls.MakeTransparent(unlockWindow.BackColor);
                    target = new Rectangle(PointToClient(unlockWindow.Location), unlockWindow.Size);
                    graphics.DrawImageUnscaled(controls, target.Location);
                }
            }
            return target;
        }
    }
}
