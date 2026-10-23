using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace AniimoAffinities
{
    // An illustrative scene shows the pending setting without changing a real window.
    internal sealed class TransparencyPreview : CheckBox
    {
        private readonly RoundCheckBox option;
        private readonly int kind;
        private bool inverse;
        internal bool PreviewEnabled
        {
            get
            {
                return option.Checked != inverse;
            }
        }
        internal bool Inverse
        {
            get
            {
                return inverse;
            }
            set
            {
                if (inverse != value)
                {
                    inverse = value;
                    UpdateDescription();
                    Invalidate(true);
                }
            }
        }
        internal TransparencyPreview(RoundCheckBox option, int kind)
        {
            this.option = option;
            this.kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            AutoCheck = false;
            TabStop = true;
            Checked = option.Checked;
            Text = option.Text;
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleName = option.AccessibleName;
            MouseEnter += (s, e) => Inverse = true;
            MouseLeave += (s, e) => Inverse = false;
            option.CheckedChanged += Changed;
            Click += (s, e) => { if (option.Enabled) option.Checked = !option.Checked; };
            UpdateDescription();
        }
        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            if (option != null && option.Checked != Checked)
                option.Checked = Checked;
            Invalidate(true);
        }
        private void Changed(object sender, EventArgs e)
        {
            Checked = option.Checked;
            UpdateDescription();
            Invalidate(true);
        }
        private void UpdateDescription()
        {
            AccessibleDescription = PreviewEnabled ? "Preview: frosted background, opaque text and element icons. Not applied until Apply." : "Preview: opaque window. Not applied until Apply.";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                option.CheckedChanged -= Changed;
                option.Dispose();
            }
            base.Dispose(disposing);
        }
        internal void DrawPreview(Graphics graphics, RectangleF bounds)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(bounds.X, bounds.Y);
            graphics.ScaleTransform(bounds.Width / Width, bounds.Height / Height);
            OnPaint(new PaintEventArgs(graphics, ClientRectangle));
            graphics.Restore(state);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var background = new SolidBrush(Theme.Background))
                g.FillRectangle(background, ClientRectangle);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Drawing.Box(g, new RectangleF(2, 2, Width - 5, Height - 5), Checked ? Theme.AccentSurface : Theme.Surface, Checked || Focused ? Theme.Accent : Theme.Border, Theme.ControlRadius);
            using (var font = UiFonts.Create("Segoe UI", 7.5f))
                Drawing.Text(g, Text, font, Checked ? Theme.AccentForeground : Theme.Muted, new RectangleF(5, 3, Width - 10, 18), true);
            var state = g.Save();
            g.TranslateTransform(5, 24);
            g.ScaleTransform((Width - 10f) / Width, (Height - 30f) / 54);
            DrawScene(g, 54);
            g.Restore(state);
        }
        private void DrawScene(Graphics g, float height)
        {
            var scene = new RectangleF(2, 2, Width - 5, height - 5);
            using (var clip = Drawing.Round(scene, 6))
            {
                var state = g.Save();
                g.SetClip(clip);
                using (var brush = new LinearGradientBrush(scene, Color.FromArgb(45, 137, 160), Color.FromArgb(184, 106, 85), 25))
                    g.FillRectangle(brush, scene);
                using (var pen = new Pen(Color.FromArgb(100, Color.White), 3))
                    for (int x = 0; x < Width; x += 18)
                        g.DrawLine(pen, x, 0, x - 24, height);
                if (kind == 0)
                {
                    Pane(g, new RectangleF(8, 8, 48, height - 16), false);
                    Pane(g, new RectangleF(66, 8, Width - 74, height - 16), false);
                }
                else if (kind == 1)
                {
                    Drawing.Box(g, new RectangleF(9, 7, Width - 30, height - 18), Theme.Surface, Theme.Border, 4);
                    Pane(g, new RectangleF(33, 14, Width - 43, height - 22), true);
                }
                else
                    Pane(g, new RectangleF(9, 7, Width - 18, height - 14), true);
                g.Restore(state);
            }
            using (var outline = Drawing.Round(scene, 6))
            using (var pen = new Pen(PreviewEnabled ? Theme.Accent : Theme.Border))
                g.DrawPath(pen, outline);
        }
        private void Pane(Graphics g, RectangleF bounds, bool caption)
        {
            if (PreviewEnabled)
            {
                using (var shape = Drawing.Round(bounds, 4))
                using (var blurred = new LinearGradientBrush(bounds, Color.FromArgb(77, 138, 152), Color.FromArgb(153, 117, 108), 25))
                    g.FillPath(blurred, shape);
                Drawing.Box(g, bounds, Color.FromArgb(95, Theme.Surface), Theme.StrongBorder, 4);
            }
            else
                Drawing.Box(g, bounds, Theme.Surface, Theme.Border, 4);
            if (caption)
                using (var pen = new Pen(Theme.Border))
                    g.DrawLine(pen, bounds.Left + 3, bounds.Top + 7, bounds.Right - 3, bounds.Top + 7);
            float y = bounds.Top + (caption ? 10 : 6);
            VectorIcon.Draw(g, kind == 0 ? 7 : 4, new RectangleF(bounds.Left + 5, y, 14, 14));
            using (var font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold))
                Drawing.Text(g, "Aa", font, Theme.TextColor, new RectangleF(bounds.Left + 22, y, bounds.Width - 23, 15), false);
        }
    }
}
