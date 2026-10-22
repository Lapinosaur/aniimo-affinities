using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace AniimoAffinities
{
    // One timeline for window size and both live panels. Retarget from displayed geometry.
    internal sealed class PanelTransition : IDisposable
    {
        internal sealed class Frame
        {
            internal Rectangle Window, Board, Content;
            internal bool Visible;
            internal float Scale;
            internal Rectangle[] Cards;
            internal Bitmap Image;
        }
        private readonly PortableWindow window;
        private readonly Control body, board, content;
        private readonly Timer timer = new Timer { Interval = 15 };
        private readonly Stopwatch watch = new Stopwatch();
        private Frame origin, destination;
        private DockStyle boardDock, contentDock;
        private bool running;
        private Action pending;
        private readonly TransitionSurface surface = new TransitionSurface { Visible = false };
        internal bool Defer(Action action)
        {
            if (!running)
                return false;
            pending = action;
            return true;
        }
        internal PanelTransition(PortableWindow owner, Control parent, Control panel, Control canvas)
        {
            window = owner;
            body = parent;
            board = panel;
            content = canvas;
            body.Controls.Add(surface);
            timer.Tick += (s, e) => Advance(Math.Min(1, watch.Elapsed.TotalMilliseconds / 180));
        }
        internal Frame Capture(bool image = false)
        {
            var frame = new Frame { Window = window.Bounds, Board = board.Bounds, Content = content.Bounds, Visible = running ? origin.Visible || destination.Visible : board.Visible, Scale = window.AppliedWindowScale, Cards = board.Controls.Cast<Control>().Select(c => c.Bounds).ToArray() };
            if (image && window.Visible && body.Width > 0 && body.Height > 0)
            {
                frame.Image = new Bitmap(body.Width, body.Height);
                body.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, body.Size));
            }
            return frame;
        }
        internal void Start(Frame previous, Rectangle targetWindow)
        {
            // Resolve the final docking geometry while the enclosing transaction is suspended.
            window.PerformLayout();
            body.PerformLayout();
            if (!window.Visible || !SystemInformation.IsMenuAnimationEnabled || Math.Abs(previous.Scale - window.AppliedWindowScale) > .001f)
            {
                window.Bounds = targetWindow;
                body.Dock = DockStyle.Fill;
                if (previous.Image != null)
                    previous.Image.Dispose();
                return;
            }
            destination = Capture(true);
            destination.Window = targetWindow;
            surface.Before = previous.Image;
            surface.After = destination.Image;
            surface.Visible = true;
            surface.BringToFront();
            boardDock = board.Dock;
            contentDock = content.Dock;
            origin = previous;
            if (!origin.Visible)
                origin.Board = Outside(destination.Board, boardDock, origin.Content.Size);
            if (!destination.Visible)
                destination.Board = Outside(origin.Board, boardDock, destination.Content.Size);
            var team = board as TeamBoard;
            if (team != null)
                team.TransitionLayout = true;
            board.Dock = content.Dock = DockStyle.None;
            body.Dock = DockStyle.Fill;
            board.Visible = false;
            content.Visible = false;
            running = true;
            watch.Restart();
            Advance(0);
            timer.Start();
        }
        private static Rectangle Outside(Rectangle rectangle, DockStyle dock, Size area)
        {
            if (dock == DockStyle.Left)
                rectangle.X = -rectangle.Width;
            else if (dock == DockStyle.Right)
                rectangle.X = area.Width;
            else if (dock == DockStyle.Top)
                rectangle.Y = -rectangle.Height;
            else
                rectangle.Y = area.Height;
            return rectangle;
        }
        internal void Advance(double progress)
        {
            if (!running)
                return;
            if (progress >= 1)
            {
                Finish();
                return;
            }
            // Interrupted transitions start at the actual last displayed frame.
            double t = SwapBezier(progress);
            surface.Progress = progress;
            using (var update = new VisualUpdate(window))
            {
                var nextWindow = Blend(origin.Window, destination.Window, t);
                nextWindow.X = destination.Window.Right - nextWindow.Width;
                nextWindow.Y = destination.Window.Top;
                window.Bounds = nextWindow;
                update.LayoutNow();
                board.Bounds = Blend(origin.Board, destination.Board, t);
                content.Bounds = Blend(origin.Content, destination.Content, t);
                for (int i = 0; i < board.Controls.Count; i++)
                    board.Controls[i].Bounds = Blend(origin.Cards[i], destination.Cards[i], t);
                surface.Bounds = body.ClientRectangle;
                surface.BringToFront();
                surface.Invalidate();
            }
        }
        private static double SwapBezier(double progress)
        {
            if (progress <= 0)
                return 0;
            if (progress >= 1)
                return 1;
            // cubic-bezier(0.22, 1, 0.36, 1): invert X (elapsed time), then evaluate Y.
            double low = 0, high = 1;
            for (int i = 0; i < 20; i++)
            {
                double u = (low + high) / 2, v = 1 - u;
                double x = 3 * v * v * u * .22 + 3 * v * u * u * .36 + u * u * u;
                if (x < progress)
                    low = u;
                else
                    high = u;
            }
            double remaining = 1 - (low + high) / 2;
            return 1 - remaining * remaining * remaining;
        }
        private static Rectangle Blend(Rectangle a, Rectangle b, double t)
        {
            return new Rectangle(Mix(a.X, b.X, t), Mix(a.Y, b.Y, t), Mix(a.Width, b.Width, t), Mix(a.Height, b.Height, t));
        }
        private static int Mix(int a, int b, double t)
        {
            return (int)Math.Round(a + (b - a) * t);
        }
        internal void Finish()
        {
            if (!running)
                return;
            timer.Stop();
            running = false;
            using (new VisualUpdate(window))
            {
                window.Bounds = destination.Window;
                board.Visible = destination.Visible;
                board.Bounds = destination.Board;
                content.Bounds = destination.Content;
                surface.Hide();
                content.Visible = true;
                surface.ReleaseImages();
                board.Dock = boardDock;
                content.Dock = contentDock;
                var team = board as TeamBoard;
                if (team != null)
                {
                    team.TransitionLayout = false;
                    team.RefreshCards();
                }
            }
            var next = pending;
            pending = null;
            if (next != null)
                next();
        }
        public void Dispose()
        {
            pending = null;
            timer.Dispose();
            surface.Dispose();
        }
    }
}
