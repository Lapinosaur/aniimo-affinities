using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class NameInput : Panel
    {
        private sealed class HintTextBox : TextBox
        {
            internal string Hint;
            protected override void WndProc(ref Message message)
            {
                base.WndProc(ref message);
                if ((message.Msg != 0x000F && message.Msg != 0x0318) || TextLength != 0 || Focused || string.IsNullOrEmpty(Hint))
                    return;
                using (var graphics = message.Msg == 0x0318 ? Graphics.FromHdc(message.WParam) : Graphics.FromHwnd(Handle))
                    TextRenderer.DrawText(graphics, Hint, Font, ClientRectangle, Theme.Muted,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
        }
        internal readonly TextBox Editor = new HintTextBox();
        private bool hover;
        internal string NavigationHint = "";
        internal NameInput(string value, string hint, string accessibleName)
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            TabStop = false;
            Size = new Size(140, 36);
            Editor.BorderStyle = BorderStyle.None;
            Editor.MaxLength = 40;
            Editor.Text = value;
            Editor.AccessibleName = accessibleName;
            Editor.BackColor = Theme.Surface;
            Editor.ForeColor = Theme.TextColor;
            ((HintTextBox)Editor).Hint = hint;
            Controls.Add(Editor);
            Editor.GotFocus += (s, e) => RefreshState();
            Editor.LostFocus += (s, e) => RefreshState();
            Editor.TextChanged += (s, e) => RefreshState();
            foreach (Control control in new Control[] { this, Editor })
            {
                control.MouseEnter += (s, e) => { hover = true; Invalidate(); };
                control.MouseLeave += (s, e) => { hover = ClientRectangle.Contains(PointToClient(Cursor.Position)); Invalidate(); };
            }
            Click += (s, e) => Editor.Focus();
            Resize += (s, e) => Arrange();
            FontChanged += (s, e) => Arrange();
            Editor.FontChanged += (s, e) => Arrange();
            Arrange();
            RefreshState();
        }
        private void Arrange()
        {
            // Keep native child windows below the badge strip: TextBox cannot be transparent.
            int inset = Math.Max(1, (int)Math.Round(11 * WindowZoom.Factor(this)));
            int top = Math.Max((int)Math.Ceiling(12 * WindowZoom.Factor(this)), (Height - Editor.PreferredHeight) / 2);
            Editor.SetBounds(inset, top, Math.Max(1, Width - inset * 2), Editor.PreferredHeight);
        }
        private void RefreshState()
        {
            Editor.Invalidate();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Editor.BackColor = Theme.Surface;
            Editor.ForeColor = Theme.TextColor;
            Drawing.Box(e.Graphics, new RectangleF(1, 1, Width - 3, Height - 3), Theme.Surface,
                Editor.Focused ? Theme.Accent : hover ? Theme.StrongBorder : Theme.Border, Theme.ControlRadius);
            if (DisplaySettings.ShowBadges && NavigationHint.Length > 0)
            {
                float zoom = WindowZoom.Factor(this);
                float width = (NavigationHint.Length * 5 + 10) * zoom;
                var bounds = new RectangleF(Width - width - 5 * zoom, 2 * zoom, width, 9 * zoom);
                Drawing.Box(e.Graphics, bounds, Theme.Border, Theme.Border, 3 * zoom);
                using (var font = UiFonts.Create("Segoe UI", 6 * zoom, FontStyle.Bold))
                    Drawing.Text(e.Graphics, NavigationHint, font, Theme.TextColor, bounds, true);
            }
        }
    }
}
