using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class DisplayChoice : RadioButton
    {
        internal float PreviewScale = 1;
        internal bool WindowPreview, BadgePreview, PreviewBadges;
        internal Func<int> PreviewType;
        internal Func<Keys[]> PreviewBindings;
        internal DisplayChoice()
        {
            Appearance = Appearance.Button;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        protected override void OnChangeUICues(UICuesEventArgs e)
        {
            base.OnChangeUICues(e);
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Background);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Drawing.Box(g, new RectangleF(2, 2, Width - 5, Height - 5), Checked ? Theme.AccentSurface : Theme.Surface, Checked ? Theme.Accent : Theme.Border, Theme.ControlRadius);
            if (Focused && ShowFocusCues)
                using (var outline = Drawing.Round(new RectangleF(0, 0, Width - 1, Height - 1), Theme.ControlRadius + 2))
                using (var pen = new Pen(Theme.AccentForeground))
                    g.DrawPath(pen, outline);
            using (var label = UiFonts.Create("Segoe UI", 7.5f))
                Drawing.Text(g, Text, label, Checked ? Theme.AccentForeground : Theme.Muted, new RectangleF(8, 5, Width - 16, 18), true);
            if (BadgePreview)
            {
                float left = (Width - 42) / 2f;
                Drawing.Box(g, new RectangleF(left, 27, 42, 38), Theme.Background, Theme.Border, 6);
                int type = PreviewType();
                VectorIcon.Draw(g, type, new RectangleF(left + 8, 32, 20, 20));
                var main = new RectangleF(left + 15, 53, 25, 10);
                Drawing.Box(g, main, Theme.AccentSurface, Theme.AccentSurface, 3);
                using (var font = UiFonts.Create("Segoe UI", 6, FontStyle.Bold))
                    Drawing.Text(g, "Main", font, Theme.AccentForeground, main, true);
                string shortcut = KeyBindings.Label(PreviewBindings()[Array.IndexOf(KeyBindings.Actions, Model.Names[type])]);
                if (PreviewBadges && shortcut.Length > 0)
                {
                    float width = Math.Max(12, shortcut.Length * 6 + 4);
                    var key = new RectangleF(left + 42 - width, 25, width, 12);
                    Drawing.Box(g, key, Theme.Border, Theme.Border, 3);
                    using (var font = UiFonts.Create("Segoe UI", 6.5f, FontStyle.Bold))
                        Drawing.Text(g, shortcut, font, Theme.TextColor, key, true);
                }
                PaintAvailability(g);
                return;
            }
            if (WindowPreview)
            {
                float width = 36 * PreviewScale, height = 22 * PreviewScale, left = (Width - width) / 2f, top = 62 - height;
                var ink = Checked ? Theme.AccentForeground : Theme.Muted;
                Drawing.Box(g, new RectangleF(left, top, width, height), Theme.Background, ink, 3);
                using (var pen = new Pen(ink))
                    g.DrawLine(pen, left + 1, top + height * .23f, left + width - 1, top + height * .23f);
                using (var fill = new SolidBrush(Theme.Surface))
                {
                    g.FillRectangle(fill, left + width * .08f, top + height * .36f, width * .38f, height * .5f);
                    g.FillRectangle(fill, left + width * .54f, top + height * .36f, width * .38f, height * .5f);
                }
                var host = TopLevelControl as SettingsDialog;
                if (DisplaySettings.ShowBadges && (host == null || host.KeyboardNavigation))
                {
                    string hint = Math.Abs(PreviewScale - DisplaySettings.WindowScale - .25f) < .01 ? "Ctrl+↑" : Math.Abs(PreviewScale - DisplaySettings.WindowScale + .25f) < .01 ? "Ctrl+↓" : "";
                    if (hint.Length > 0)
                    {
                        var badge = new RectangleF(Width - 49, 22, 44, 13);
                        Drawing.Box(g, badge, Theme.Border, Theme.Border, 3);
                        using (var font = UiFonts.Create("Segoe UI", 6.5f, FontStyle.Bold))
                            Drawing.Text(g, hint, font, Theme.TextColor, badge, true);
                    }
                }
                PaintAvailability(g);
                return;
            }
            using (var title = new Font("Segoe UI", 13 * PreviewScale, FontStyle.Bold))
                Drawing.Text(g, "Attack", title, Theme.Attack, new RectangleF(12, 25, 104, 28), false);
            using (var body = new Font("Segoe UI", 9 * PreviewScale))
                Drawing.Text(g, "Damage dealt", body, Theme.TextColor, new RectangleF(12, 53, 104, 22), false);
            using (var number = new Font("Segoe UI", 8 * PreviewScale, FontStyle.Bold))
                Drawing.Text(g, "×1.6  ·  ×0.625", number, Theme.Muted, new RectangleF(12, 75, 104, 20), false);
            PaintAvailability(g);
        }
        private void PaintAvailability(Graphics graphics)
        {
            if (!Enabled)
                using (var shade = new SolidBrush(Color.FromArgb(110, Theme.Surface)))
                using (var outline = Drawing.Round(new RectangleF(2, 2, Width - 5, Height - 5), Theme.ControlRadius))
                    graphics.FillPath(shade, outline);
        }
    }
}
