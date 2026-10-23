using System;
using System.Drawing;

namespace AniimoAffinities
{
    internal sealed partial class MainForm
    {
        protected override void OnPinnedChanged()
        {
            RefreshOverlayHeader();
        }

        // The title is not painted while pinned. Keep only the toolbar's space,
        // and none when locked (its controls live in the floating unlock bar).
        private void RefreshOverlayHeader()
        {
            if (affinityHeader == null)
                return;
            FinishTeamTransition();
            using (new VisualUpdate(this))
            {
                int nextHeight = (int)Math.Round((OverlayLocked || TopMost && childView ? 0 : TopMost ? 48 : 66) * AppliedWindowScale);
                int difference = nextHeight - affinityHeader.Height;
                // Remove the unused space from the window, not just its layout.
                // Lower the minimum first so compact overlays are not clamped back
                // to the normal window height. The inverse restores both sizes.
                int nextWindowHeight = Height + difference;
                MinimumSize = new Size(MinimumSize.Width, Math.Max(1, MinimumSize.Height + difference));
                affinityHeader.Height = nextHeight;
                Height = nextWindowHeight;
                if (diagramSize.Height > 0)
                    diagramSize.Height += (int)Math.Round(difference / AppliedWindowScale);
                affinityHeader.Invalidate();
            }
        }
    }
}
