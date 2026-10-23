using System;
using System.Linq;

namespace AniimoAffinities
{
    internal sealed partial class MainForm
    {
        internal void PrepareRadarTutorial()
        {
            if (!TutorialSnapshot)
                throw new InvalidOperationException("Tutorial snapshot required.");
            int[] main = { 7, 2, 0 }, sub = { 4, 8, 5 };
            for (int i = 0; i < 3; i++)
            {
                teamPlan.Allies[i].Main = main[i];
                teamPlan.Allies[i].Sub = sub[i];
            }
            teamPlan.Visible = true;
            teamPlan.Position = "Top";
            teamBoard.RefreshCards();
            ApplyTeamLayout();
            FinishTeamTransition();
            if (!inlineRadar)
                ToggleInlineRadar();
            teamBoard.Controls.OfType<TeamCard>().First().PerformClick();
        }
        internal int TutorialComparisonCount
        {
            get
            {
                return ComparisonSeries().Length;
            }
        }
        internal int TutorialHighlightedSlot
        {
            get
            {
                return teamPreview;
            }
        }
    }
}
