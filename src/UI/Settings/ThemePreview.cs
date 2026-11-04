using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed class ThemePreview : RadioButton
    {
        internal int Mode;
        internal string Shortcut = "";
        internal bool ShowShortcut;
        private bool movingFocus;
        internal void FocusPreview()
        {
            bool autoCheck = AutoCheck;
            movingFocus = true;
            AutoCheck = false;
            try
            {
                Focus();
            }
            finally { AutoCheck = autoCheck; movingFocus = false; }
        }
        protected override void OnClick(System.EventArgs e)
        {
            if (!movingFocus)
                base.OnClick(e);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Events.Dispose();
        }
        internal ThemePreview()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            PreviewCard.Attach(this);
            MouseEnter += (s, e) => Invalidate();
            MouseLeave += (s, e) => Invalidate();
            GotFocus += (s, e) => Invalidate();
            LostFocus += (s, e) => Invalidate();
            CheckedChanged += (s, e) => Invalidate();
            ChangeUICues += (s, e) => Invalidate();
            EnabledChanged += (s, e) => { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Background);
            float zoom = WindowZoom.Factor(this);
            g.ScaleTransform(zoom, zoom);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Width / zoom, h = Height / zoom;
            PreviewCard.Draw(g, new RectangleF(0, 0, w - 1, h - 1), Checked, Enabled, ClientRectangle.Contains(PointToClient(Cursor.Position)), Focused && ShowFocusCues);
            using (var font = UiFonts.Create("Segoe UI", 7))
                Drawing.Text(g, Text, font, PreviewCard.LabelColor(Checked, Enabled), new RectangleF(4, 1, w - 8, 15), true);
            int count = Mode == 1 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                bool light = Mode == 2 || Mode == 1 && i == 1;
                Color background = light ? Theme.Ivory : Theme.Anthracite, ink = light ? Theme.Anthracite : Theme.Ivory;
                float cell = (w - 16) / count;
                Drawing.Box(g, new RectangleF(7 + i * cell, 18, cell - 2, h - 24), background, Color.FromArgb(110, ink), 4);
                VectorIcon.Draw(g, 7, new RectangleF(11 + i * cell, 21, 14, 14));
                using (var font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold))
                    Drawing.Text(g, "Aa", font, ink, new RectangleF(28 + i * cell, 18, cell - 30, h - 24), false);
            }
            if (ShowShortcut && Shortcut.Length > 0)
                ShortcutText.DrawBadge(g, Shortcut, w);
        }
    }
}
