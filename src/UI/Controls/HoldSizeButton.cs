using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace AniimoAffinities
{
    internal sealed class HoldSizeButton : InteractiveButton
    {
        internal float Target;
        internal string Glyph = "";
        internal event Action<float> Commit;
        private readonly Timer timer = new Timer { Interval = 16 };
        private readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private float progress;
        private readonly KeyboardHold keyboard;
        internal HoldSizeButton()
        {
            AccessibleDescription = "Hold Space or the left mouse button for 0.9 seconds to apply. Release early to cancel.";
            EnabledChanged += (s, e) => { if (!Enabled) CancelHold(); };
            VisibleChanged += (s, e) => { if (!Visible) CancelHold(); };
            LostFocus += (s, e) => CancelHold();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            keyboard = new KeyboardHold(this, () => { watch.Restart(); timer.Start(); }, CancelHold);
            timer.Tick += (s, e) =>
            {
                progress = Math.Min(1, (float)watch.Elapsed.TotalMilliseconds / 900);
                Invalidate();
                if (progress >= 1)
                {
                    float target = Target;
                    CancelHold();
                    if (Commit != null)
                        Commit(target);
                }
            };
        }
        internal void CancelHold()
        {
            timer.Stop();
            watch.Reset();
            progress = 0;
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Enabled && e.Button == MouseButtons.Left)
            {
                watch.Restart();
                timer.Start();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            CancelHold();
            base.OnMouseUp(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            if (!keyboard.Active)
                CancelHold();
            base.OnMouseLeave(e);
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (!Capture && !keyboard.Active)
                CancelHold();
            base.OnMouseCaptureChanged(e);
        }
        protected override void OnClick(EventArgs e)
        {
        } // A click alone must never change scale.
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent == null ? Theme.Background : Parent.BackColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Drawing.Box(g, new RectangleF(1, 1, Width - 3, Height - 3), Theme.Surface, Theme.Border, 7);
            if (Glyph.Length > 0)
                Glyphs.Draw(g, Glyph, new RectangleF((Width - 18) / 2f, (Height - 18) / 2f, 18, 18), Theme.TextColor);
            else
                using (var font = UiFonts.Create("Segoe UI", 8, FontStyle.Bold))
                    Drawing.Text(g, Text, font, Theme.TextColor, new RectangleF(0, 0, Width, Height), true);
            Drawing.HoldHint(g, new RectangleF(1, 1, Width - 3, Height - 3));
            FinishPaint(g, Width, Height);
            if (progress <= 0 || !Enabled)
                return;
            Drawing.ProgressOutline(g, new RectangleF(1, 1, Width - 3, Height - 3), 7, progress, Theme.Accent, 2);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
