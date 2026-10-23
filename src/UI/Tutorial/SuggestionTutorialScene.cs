using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SuggestionTutorialScene : ITutorialScene
    {
        private sealed class SnapshotHost : Form
        {
            protected override bool ShowWithoutActivation
            {
                get
                {
                    return true;
                }
            }
        }
        private readonly string view;
        internal readonly TeamPlan Plan = new TeamPlan();
        private readonly Form host;
        private readonly TeamBoard board;
        private readonly TipBubble bubble = new TipBubble { Opacity = 0 };
        private SuggestionRadarDialog radar;
        private TeamCard card;
        private int phase;

        internal SuggestionTutorialScene(string view)
        {
            this.view = view;
            Plan.Allies[0].Main = 7;
            Plan.Allies[0].Sub = 4;
            Plan.Allies[1].Main = 2;
            Plan.Allies[1].Sub = 8;
            Plan.Allies[2].Main = 0;
            if (view == "suggest-counter")
            {
                Plan.Opponents[0].Main = 0;
                Plan.Opponents[0].Sub = 5;
                Plan.Opponents[1].Main = 7;
            }
            host = new SnapshotHost { FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(900, 650), Opacity = 0, ShowInTaskbar = false, BackColor = Theme.Background };
            board = new TeamBoard(Plan, slot => { }) { Bounds = new Rectangle(10, 24, 880, 100) };
            host.Controls.Add(board);
            board.RefreshCards();
            card = board.Controls.OfType<TeamCard>().ElementAt(3);
            board.SuggestionPreviewRequested += slot =>
            {
                radar = new SuggestionRadarDialog(Plan, slot, card.Suggestion, new int[0]) { TutorialSnapshot = true, Opacity = 0 };
                radar.Show(host);
            };
            host.Show();
            bubble.SetApplyAction(() => card.SuggestButton.PerformClick());
            bubble.SetPreviewAction(() => card.PreviewButton.PerformClick());
            bubble.SetDismissAction(() => bubble.Hide());
        }

        public void Advance(int nextPhase)
        {
            phase = nextPhase;
            if (phase == 1)
            {
                card.Selected = true;
                if (view == "suggest-radar")
                    card.PreviewButton.PerformClick();
            }
            if (phase == 2)
            {
                if (view == "suggest-apply")
                    card.SuggestButton.PerformClick();
                if (radar != null)
                    radar.HighlightedSlot = 3;
            }
        }

        public TutorialOverlay.Frame Capture()
        {
            var frame = new TutorialOverlay.Frame { Image = new Bitmap(host.Width, host.Height) };
            host.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, host.Size));
            frame.Targets["suggestion"] = new Rectangle(board.Left + card.Left, board.Top + card.Top, card.Width, card.Height);
            if (radar != null)
            {
                using (var image = new Bitmap(radar.Width, radar.Height))
                using (var graphics = Graphics.FromImage(frame.Image))
                {
                    radar.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    graphics.DrawImage(image, new Rectangle(0, 0, host.Width, host.Height));
                }
                frame.Targets["suggestion"] = new Rectangle(12, 64, 870, 130);
            }
            else if (phase > 0)
            {
                var proposal = Plan.Allies[3].Known ? TeamSuggestions.ComplementaryAdditions(Plan)[4] : card.Suggestion;
                if (proposal != null)
                {
                    bubble.Hide();
                    bubble.Present(card, SuggestionGraphic.Title(proposal), null, null,
                        (g, bounds) => SuggestionGraphic.Draw(g, bounds, proposal, Plan.Allies[3].Known ? "Remaining proposals recalculated" : "Preview only: not applied"), SuggestionGraphic.Size(proposal));
                    using (var image = new Bitmap(bubble.Width, bubble.Height))
                    using (var graphics = Graphics.FromImage(frame.Image))
                    {
                        bubble.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        graphics.DrawImageUnscaled(image, 24, 140);
                    }
                    frame.Targets["suggestion"] = new Rectangle(24, 140, bubble.Width, bubble.Height);
                }
            }
            if (view == "suggest-apply" && Plan.Allies[3].Known)
                frame.Targets["suggestion"] = board.Bounds;
            return frame;
        }

        public void Dispose()
        {
            if (radar != null)
                radar.Dispose();
            bubble.Dispose();
            host.Dispose();
        }
    }
}
