using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class TutorialOverlay
    {
        private readonly Timer suggestionAnimationTimer = new Timer { Interval = 100 };
        private readonly Stopwatch suggestionAnimationClock = new Stopwatch();
        private ITutorialScene suggestionScene;
        private string suggestionView;
        private int suggestionPhase = -1;

        private ITutorialScene CreateAnimatedScene(string view)
        {
            return view.StartsWith("radar-", StringComparison.Ordinal) ? (ITutorialScene)new RadarTutorialScene(source, view) : new SuggestionTutorialScene(view);
        }

        private void StartSuggestionAnimation(string view)
        {
            if (!view.StartsWith("suggest-", StringComparison.Ordinal) && !view.StartsWith("radar-", StringComparison.Ordinal))
                return;
            suggestionView = view;
            suggestionScene = CreateAnimatedScene(view);
            suggestionAnimationClock.Restart();
            suggestionAnimationTimer.Tick += AnimateSuggestion;
            AnimateSuggestion(null, EventArgs.Empty);
            suggestionAnimationTimer.Start();
        }

        private void AnimateSuggestion(object sender, EventArgs e)
        {
            if (suggestionScene == null || !Visible)
                return;
            int phase = (int)(suggestionAnimationClock.ElapsedMilliseconds % 7500 / 2500);
            if (phase == suggestionPhase)
                return;
            if (phase == 0 && suggestionPhase >= 0)
            {
                suggestionScene.Dispose();
                suggestionScene = CreateAnimatedScene(suggestionView);
            }
            suggestionPhase = phase;
            suggestionScene.Advance(phase);
            var previousFrame = frames[suggestionView];
            frames[suggestionView] = suggestionScene.Capture();
            previousFrame.Dispose();
            ArrangeStep();
            Invalidate();
        }

        private void StopSuggestionAnimation()
        {
            suggestionAnimationTimer.Stop();
            suggestionAnimationTimer.Tick -= AnimateSuggestion;
            suggestionAnimationClock.Reset();
            suggestionPhase = -1;
            if (suggestionScene != null)
            {
                suggestionScene.Dispose();
                suggestionScene = null;
            }
        }
    }
}
