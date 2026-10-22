using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal partial class PortableWindow
    {
        private Button CaptionButton(string text, string label, bool danger = false, bool pin = false)
        {
            var button = new Button { Text = "", AccessibleName = label, Dock = DockStyle.Right, Width = 42, FlatStyle = FlatStyle.Flat, ForeColor = Theme.Muted, BackColor = Theme.Background, TabStop = true };
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Theme.Background;
            button.FlatAppearance.MouseDownBackColor = Theme.Background;
            bool hover = false, pressed = false;
            button.MouseEnter += (s, e) => { hover = true; button.Invalidate(); };
            button.MouseLeave += (s, e) => { hover = false; pressed = false; button.Invalidate(); };
            button.MouseDown += (s, e) => { pressed = e.Button == MouseButtons.Left; button.Invalidate(); };
            button.MouseUp += (s, e) => { pressed = false; button.Invalidate(); };
            button.GotFocus += (s, e) => button.Invalidate();
            button.LostFocus += (s, e) => button.Invalidate();
            button.Paint += (s, e) =>
            {
                float zoom = AppliedWindowScale;
                int bw = (int)Math.Round(button.Width / zoom), bh = (int)Math.Round(button.Height / zoom);
                var g = e.Graphics;
                g.ScaleTransform(zoom, zoom);
                g.Clear(Theme.Background);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (text.Length == 0 && !pin)
                    return;
                var bounds = new RectangleF((bw - 28) / 2f, (bh - 28) / 2f, 28, 28);
                bool active = pin && TopMost;
                Color fill = danger && (TopMost || hover) ? Color.FromArgb(185, 45, 55) : active ? Theme.AccentSurface : Theme.Surface;
                if (hover)
                    fill = danger ? Color.FromArgb(225, 65, 75) : active ? Theme.AccentHover : Theme.HoverSurface;
                if (pressed)
                    fill = danger ? Color.FromArgb(185, 45, 55) : active ? Theme.AccentHover : Theme.HoverSurface;
                bool hasHold = pin || text == "matrix" || text == "radar";
                Drawing.Box(g, bounds, fill, active ? Theme.Accent : hasHold ? Theme.Border : fill, Theme.ControlRadius);
                if (hasHold)
                    Drawing.HoldHint(g, bounds);
                if (button.Focused && ShowFocusCues)
                {
                    bounds.Inflate(2, 2);
                    using (var outline = Drawing.Round(bounds, Theme.ControlRadius + 2))
                    using (var pen = new Pen(Theme.AccentForeground))
                        g.DrawPath(pen, outline);
                }
                Color ink = danger && (TopMost || hover || pressed) ? Color.White : active ? Theme.AccentForeground : Theme.TextColor;
                if (pin && OverlayLocked)
                    Glyphs.Draw(g, "lock", new RectangleF((bw - 16) / 2f, (bh - 16) / 2f, 16, 16), ink);
                else if (pin)
                    Glyphs.Draw(g, "pin", new RectangleF((bw - 16) / 2f, (bh - 16) / 2f, 16, 16), ink);
                else if (text == "matrix" || text == "radar")
                    Glyphs.Draw(g, text == "radar" ? RadarViewGlyph : MatrixViewGlyph, new RectangleF((bw - 16) / 2f, (bh - 16) / 2f, 16, 16), ink);
                else if (text == "⚙" || text == "−")
                    Glyphs.Draw(g, text == "⚙" ? "settings" : "minimize", new RectangleF((bw - 16) / 2f, (bh - 16) / 2f, 16, 16), ink);
                else
                    Drawing.Text(g, text, button.Font, ink, new RectangleF(0, 0, bw, bh), true);
                Keys shortcut = danger && CompactCaption ? Keys.Escape : text == "matrix" ? MatrixShortcut : text == "radar" ? RadarShortcut : Keys.None;
                if (DisplaySettings.ShowBadges && shortcut != Keys.None && Form.ActiveForm == this)
                {
                    string key = shortcut == Keys.Escape ? "Esc" : KeyBindings.Label(shortcut);
                    int width = Math.Min(bw - 2, Math.Max(15, key.Length * 5 + 4));
                    var badge = new RectangleF(bw - width - 1, 3, width, 12);
                    Drawing.Box(g, badge, Theme.Border, Theme.Border, 3);
                    using (var font = UiFonts.Create("Segoe UI", key.Length > 7 ? 5.5f : 6.5f, FontStyle.Bold))
                        Drawing.Text(g, key, font, Theme.TextColor, badge, true);
                }
                float progress = pin ? holdProgress : ((text == "radar" && !holdingMatrix) || (text == "matrix" && holdingMatrix)) ? radarHoldProgress : 0;
                if (progress > 0)
                {
                    using (var path = Drawing.Round(new RectangleF((bw - 32) / 2f, (bh - 32) / 2f, 32, 32), Theme.ControlRadius + 2))
                    {
                        path.Flatten();
                        var points = path.PathPoints;
                        float length = 0;
                        for (int i = 0; i < points.Length; i++)
                        {
                            var a = points[i];
                            var b = points[(i + 1) % points.Length];
                            length += (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                        }
                        float remaining = length * progress;
                        using (var pen = new Pen(Theme.Accent, 2))
                            for (int i = 0; i < points.Length && remaining > 0; i++)
                            {
                                var a = points[i];
                                var b = points[(i + 1) % points.Length];
                                float segment = (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                                if (segment <= 0)
                                    continue;
                                float fraction = Math.Min(1, remaining / segment);
                                g.DrawLine(pen, a, new PointF(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction));
                                remaining -= segment;
                            }
                    }
                }
            };
            return button;
        }
        // Inputs: native hit tests. Output: resize borders for the custom frame. Fallback: native window handling (including close/Alt+F4). Raises: none.
        protected override void WndProc(ref Message message)
        {
            // Retain the first native sound, but swallow repeated blocked clicks while it may still be playing.
            if (message.Msg == 0x20 && (short)(message.LParam.ToInt64() & 0xffff) == -2)
            {
                var blocking = BlockingDialog();
                if (blocking != null)
                {
                    int mouseMessage = (int)((message.LParam.ToInt64() >> 16) & 0xffff);
                    if (mouseMessage == 0x201 || mouseMessage == 0x204 || mouseMessage == 0x207 || mouseMessage == 0xA1)
                    {
                        if (!blocking.RequestAttention())
                        {
                            message.Result = new IntPtr(1);
                            return;
                        }
                    }
                }
            }
            if (message.Msg == 0x312 && message.WParam.ToInt32() == 0x414E)
            {
                SetOverlayLocked(false);
                return;
            }
            base.WndProc(ref message);
            if (customCaption && message.Msg == 0x216 && WindowState == FormWindowState.Normal)
            {
                var proposed = (WindowRect)Marshal.PtrToStructure(message.LParam, typeof(WindowRect));
                var destination = Screen.FromPoint(Cursor.Position).WorkingArea;
                var bounds = KeepCaptionVisible(Rectangle.FromLTRB(proposed.Left, proposed.Top, proposed.Right, proposed.Bottom), destination, CompactCaption ? 36 : 54);
                proposed.Left = bounds.Left;
                proposed.Top = bounds.Top;
                proposed.Right = bounds.Right;
                proposed.Bottom = bounds.Bottom;
                Marshal.StructureToPtr(proposed, message.LParam, false);
                message.Result = new IntPtr(1);
                return;
            }
            if (!customCaption || CompactCaption || message.Msg != 0x84 || WindowState != FormWindowState.Normal || message.Result != new IntPtr(1))
                return;
            long position = message.LParam.ToInt64();
            var p = PointToClient(new Point((short)(position & 0xffff), (short)((position >> 16) & 0xffff)));
            bool left = p.X < 5, right = p.X >= ClientSize.Width - 5, top = p.Y < 5, bottom = p.Y >= ClientSize.Height - 5;
            int hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 1;
            message.Result = new IntPtr(hit);
        }
    }
}
