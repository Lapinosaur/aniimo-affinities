using System;
using System.Drawing;
using System.Linq;

namespace AniimoAffinities
{
    internal interface ITutorialScene : IDisposable
    {
        void Advance(int phase);
        TutorialOverlay.Frame Capture();
    }

    internal sealed class RadarTutorialScene : ITutorialScene
    {
        internal readonly MainForm Host;
        private readonly TeamCard[] cards;
        private readonly string view;
        internal RadarTutorialScene(MainForm source, string view)
        {
            this.view = view;
            Host = source.CreateTutorialPreview();
            Host.PrepareRadarTutorial();
            Host.Opacity = 0;
            Host.ShowInTaskbar = false;
            Host.Show();
            var board = FindBoard(Host);
            cards = board.Controls.OfType<TeamCard>().ToArray();
            if (view == "radar-hover")
                foreach (var card in cards.Take(3))
                    card.CompareCheck.Checked = true;
        }
        private static TeamBoard FindBoard(System.Windows.Forms.Control root)
        {
            foreach (System.Windows.Forms.Control child in root.Controls)
            {
                var board = child as TeamBoard ?? FindBoard(child);
                if (board != null)
                    return board;
            }
            return null;
        }
        public void Advance(int phase)
        {
            if (view == "radar-read")
                cards[phase % 3].PerformClick();
            else if (view == "radar-select")
                for (int i = 0; i < 3; i++)
                    cards[i].CompareCheck.Checked = i < phase;
            else
            {
                foreach (var card in cards.Take(3))
                    card.AnimateTutorialHover(false);
                if (phase > 0)
                    cards[phase - 1].AnimateTutorialHover(true);
            }
        }
        public TutorialOverlay.Frame Capture()
        {
            var frame = new TutorialOverlay.Frame { Image = new Bitmap(Host.Width, Host.Height) };
            Host.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, Host.Size));
            frame.Targets["radars"] = Rectangle.Union(Host.TutorialTarget("team"), Host.TutorialTarget("content"));
            return frame;
        }
        public void Dispose()
        {
            Host.Dispose();
        }
    }
}
