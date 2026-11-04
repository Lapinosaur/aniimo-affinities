namespace AniimoAffinities
{
    internal partial class PortableWindow
    {
        internal float? LocalWindowScale;
        internal bool IsContentWindow
        {
            get
            {
                return ContentOnly || this is RadarDialog || this is MatrixDialog || this is SuggestionsDialog || this is SuggestionRadarDialog;
            }
        }
        internal void ApplyWindowScale()
        {
            if (!customCaption || !(this is MainForm || IsContentWindow))
                return;
            if (IsContentWindow && !LocalWindowScale.HasValue)
                LocalWindowScale = DisplaySettings.WindowScale;
            WindowZoom.Apply(this, LocalWindowScale ?? DisplaySettings.WindowScale);
            RefreshWindowFrame();
            PositionUnlockWindow();
            if (unlockWindow != null)
                unlockWindow.RefreshSizes();
        }
    }
}
