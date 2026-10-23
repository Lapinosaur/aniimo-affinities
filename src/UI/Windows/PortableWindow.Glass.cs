namespace AniimoAffinities
{
    internal partial class PortableWindow
    {
        private LockedGlass lockedGlass;
        private GlassPool glassPool;
        private InteractiveGlass interactiveGlass;
        private bool glassDisposed;
        private object glassPreviewOwner;
        private bool? glassPreviewEnabled;
        internal void PreviewWindowGlass(object owner, bool enabled)
        {
            if (glassDisposed || IsDisposed)
                return;
            if (ReferenceEquals(glassPreviewOwner, owner) && glassPreviewEnabled == enabled)
                return;
            glassPreviewOwner = owner;
            glassPreviewEnabled = enabled;
            RefreshGlassPreference();
        }
        internal void EndWindowGlassPreview(object owner)
        {
            if (!ReferenceEquals(glassPreviewOwner, owner))
                return;
            glassPreviewOwner = null;
            glassPreviewEnabled = null;
            RefreshGlassPreference();
        }
        private void RefreshGlassPreference()
        {
            // Tutorial snapshots deliberately show an invisible offscreen host.
            if (Opacity == 0 && !OverlayLocked && interactiveGlass == null)
                return;
            if (!glassDisposed && TopLevel && IsHandleCreated && Visible)
                UpdateGlass(OverlayLocked);
        }
        private void StopGlass()
        {
            if (lockedGlass != null)
            {
                lockedGlass.Dispose();
                lockedGlass = null;
            }
            if (interactiveGlass != null)
            {
                interactiveGlass.Dispose();
                interactiveGlass = null;
            }
        }
        protected void PrepareGlass()
        {
            if (glassPool == null)
                glassPool = new GlassPool();
        }
        private void UpdateGlass(bool locked)
        {
            StopGlass();
            if (glassDisposed || !TopLevel || TutorialSnapshot)
                return;
            if (locked)
            {
                if (TransparencyPreferences.Locked)
                    lockedGlass = new LockedGlass(this, glassPool);
            }
            else if (Visible && (glassPreviewEnabled ?? (this is MainForm ? TransparencyPreferences.Main : TransparencyPreferences.Children)))
                interactiveGlass = new InteractiveGlass(this);
        }
    }
}
