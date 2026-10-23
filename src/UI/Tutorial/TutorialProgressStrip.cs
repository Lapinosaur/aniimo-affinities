using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TutorialProgressStrip : Panel
    {
        private sealed class PointButton : Button
        {
            internal bool Complete, Active, Hover, BeforeActive, First, Last;
            internal PointButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Cursor = Cursors.Hand;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Theme.Background);
                float zoom = Height / 30f;
                g.ScaleTransform(zoom, zoom);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float middle = Width / zoom / 2f;
                if (!First)
                    using (var line = new Pen(BeforeActive || Active ? Theme.Accent : Theme.Border, 2))
                        g.DrawLine(line, 0, 15f, middle, 15f);
                if (!Last)
                    using (var line = new Pen(BeforeActive ? Theme.Accent : Theme.Border, 2))
                        g.DrawLine(line, middle, 15f, Width / zoom, 15f);
                using (var background = new SolidBrush(Theme.Background))
                    g.FillEllipse(background, middle - 11, 15f - 11, 22, 22);
                Color ink = Active || Hover || Focused ? Theme.AccentForeground : Theme.Muted;
                var circle = new RectangleF(Width / zoom / 2f - 7, 15f - 7, 14, 14);
                using (var fill = new SolidBrush(Complete ? (Hover || Active || Focused ? Theme.Accent : Theme.Border) : Theme.Background))
                    g.FillEllipse(fill, circle);
                using (var pen = new Pen(ink, 1.3f))
                    g.DrawEllipse(pen, circle);
                if (Complete)
                    using (var pen = new Pen(Hover || Active || Focused ? Theme.AccentInk : Theme.Muted, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLines(pen, new[] { new PointF(circle.X + 3, circle.Y + 7), new PointF(circle.X + 6, circle.Y + 10), new PointF(circle.X + 11, circle.Y + 4) });
                if (Active || Focused)
                {
                    circle.Inflate(4, 4);
                    using (var pen = new Pen(ink, 1.5f))
                        g.DrawEllipse(pen, circle);
                }
            }
        }
        private readonly PointButton[] points;
        private readonly string[] titles;
        private readonly Label title = new Label { TextAlign = ContentAlignment.MiddleCenter, UseMnemonic = false };
        private int active;
        internal TutorialProgressStrip(string[] names, Action<int> navigate)
        {
            titles = names;
            DoubleBuffered = true;
            points = names.Select((name, index) =>
            {
                var point = new PointButton { Text = "", AccessibleName = name, TabIndex = index };
                point.Click += (s, e) => navigate(index);
                point.MouseEnter += (s, e) => { point.Hover = true; Reveal(index); };
                point.MouseLeave += (s, e) => { point.Hover = false; Reveal(active); };
                point.GotFocus += (s, e) => Reveal(index);
                point.LostFocus += (s, e) => Reveal(active);
                Controls.Add(point);
                return point;
            }).ToArray();
            Controls.Add(title);
            Resize += (s, e) => Arrange();
        }
        internal void UpdateProgress(int current, bool[] complete)
        {
            active = current;
            BackColor = title.BackColor = Theme.Background;
            title.ForeColor = Theme.AccentForeground;
            for (int i = 0; i < points.Length; i++)
            {
                points[i].Active = i == active;
                points[i].BeforeActive = i < active;
                points[i].First = i == 0;
                points[i].Last = i == points.Length - 1;
                points[i].Complete = complete[i];
                points[i].AccessibleDescription = (i == active ? "Current tutorial. " : "") + (complete[i] ? "Completed." : "Not completed.") + " Click to open.";
                points[i].Invalidate();
            }
            Reveal(active);
        }
        private float Zoom
        {
            get
            {
                return Math.Max(.25f, Height / 58f);
            }
        }
        private int Z(int value)
        {
            return (int)Math.Round(value * Zoom);
        }
        private void Reveal(int index)
        {
            title.Text = index >= 0 && index < titles.Length ? titles[index] : "Tutorials";
            int width = Math.Min(Z(260), Math.Max(1, Width - Z(16)));
            int center = index >= 0 && index < points.Length ? points[index].Left + Z(15) : Width / 2;
            title.SetBounds(Math.Max(Z(8), Math.Min(Width - width - Z(8), center - width / 2)), 0, width, Z(24));
            Invalidate(true);
        }
        private void Arrange()
        {
            int span = Math.Min(Width - Z(40), Math.Max(0, points.Length - 1) * Z(64));
            for (int i = 0; i < points.Length; i++)
                points[i].SetBounds((Width - span) / 2 + (points.Length < 2 ? 0 : i * span / (points.Length - 1)) - Z(15), Z(25), Z(30), Z(30));
            Reveal(active);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (points.Length < 2)
                return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float start = points[0].Left + Z(15), end = points[points.Length - 1].Left + Z(15);
            using (var pen = new Pen(Theme.Border, 2))
                e.Graphics.DrawLine(pen, start, Z(40), end, Z(40));
            if (active >= 0)
                using (var pen = new Pen(Theme.Accent, 2))
                    e.Graphics.DrawLine(pen, start, Z(40), points[active].Left + Z(15), Z(40));
        }
    }
}
