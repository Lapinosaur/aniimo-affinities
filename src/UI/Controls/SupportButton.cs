using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SupportButton : Button
    {
        private bool hover, pressed;
        internal Color Surround = Color.Empty;
        internal readonly Image QrCode;
        internal SupportButton()
        {
            using (var stream = typeof(SupportButton).Assembly.GetManifestResourceStream("support-qr.png"))
            using (var source = Image.FromStream(stream))
                QrCode = new Bitmap(source);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            AccessibleName = "Buy me a carrot · Support Lapinosaur";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                QrCode.Dispose();
            base.Dispose(disposing);
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = pressed = false;
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            pressed = e.Button == MouseButtons.Left;
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
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
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            float scale = WindowZoom.Factor(this), w = Width / scale, h = Height / scale;
            g.ScaleTransform(scale, scale);
            g.Clear(Surround.IsEmpty ? Theme.Background : Surround);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool light = Theme.Background.GetBrightness() > .5f;
            Color start = light ? Color.FromArgb(45, 87, 198) : Color.FromArgb(62, 88, 194);
            Color end = light ? Color.FromArgb(82, 67, 181) : Color.FromArgb(101, 71, 178);
            if (hover)
            {
                start = Color.FromArgb(52, 109, 224);
                end = Color.FromArgb(119, 76, 205);
            }
            if (pressed)
            {
                start = Color.FromArgb(35, 65, 158);
                end = Color.FromArgb(70, 50, 145);
            }
            var bounds = new RectangleF(3, (h - 32) / 2 + (pressed ? 1 : 0), w - 6, 32);
            using (var path = Drawing.Round(bounds, 10))
            using (var fill = new LinearGradientBrush(bounds, start, end, 15))
                g.FillPath(fill, path);
            if (Focused && ShowFocusCues)
                using (var path = Drawing.Round(new RectangleF(bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height + 2), 11))
                using (var pen = new Pen(Theme.AccentForeground))
                    g.DrawPath(pen, path);
            float x = bounds.X + 10, y = bounds.Y + 7;
            using (var carrot = new GraphicsPath())
            {
                carrot.AddBezier(x + 2, y + 17, x + 1, y + 9, x + 5, y + 3, x + 10, y + 6);
                carrot.AddBezier(x + 10, y + 6, x + 15, y + 10, x + 7, y + 14, x + 2, y + 17);
                using (var fill = new SolidBrush(Color.FromArgb(255, 182, 89)))
                    g.FillPath(fill, carrot);
            }
            using (var leaf = new Pen(Color.FromArgb(162, 238, 185), 2) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(leaf, x + 10, y + 5, x + 11, y);
                g.DrawLine(leaf, x + 11, y + 5, x + 16, y + 2);
            }
            using (var font = UiFonts.Create("Segoe UI", 8.5f, FontStyle.Bold))
                Drawing.Text(g, "Buy me a carrot", font, Color.White, new RectangleF(bounds.X + 31, bounds.Y, bounds.Width - 35, bounds.Height), true);
        }
    }
}
