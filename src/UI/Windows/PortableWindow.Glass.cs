namespace AniimoAffinities
{
    internal partial class PortableWindow
    {
        private LockedGlass lockedGlass;
        private GlassPool glassPool;
        protected void PrepareGlass()
        {
            if (glassPool == null)
                glassPool = new GlassPool();
        }
        private void UpdateGlass(bool locked)
        {
            if (lockedGlass != null)
            {
                lockedGlass.Dispose();
                lockedGlass = null;
            }
            if (locked)
                lockedGlass = new LockedGlass(this, glassPool);
        }
    }
}
