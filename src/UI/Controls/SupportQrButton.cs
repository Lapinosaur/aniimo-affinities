using System.Drawing;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed class SupportQrButton : SettingsButton
    {
        internal Image Code;
        internal bool NonActivating;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Code = null; // The source owner disposes this shared image.
        }
        internal SupportQrButton()
        {
            AccessibleName = Language.T("Buy me a carrot");
            AccessibleDescription = Language.T("Scan with your phone or activate to open the support page.");
        }
        protected override void WndProc(ref Message message)
        {
            if (!NonActivating || message.Msg != 0x201)
            {
                base.WndProc(ref message);
                return;
            }
            SetStyle(ControlStyles.Selectable, false);
            try
            {
                base.WndProc(ref message);
            }
            finally { SetStyle(ControlStyles.Selectable, true); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.Surface);
            if (Code != null)
                SupportQr.Draw(e.Graphics, Code, new Rectangle(3, 3, Width - 6, Height - 6));
            if (ClientRectangle.Contains(PointToClient(Cursor.Position)) || Focused && ShowFocusCues)
                using (var path = Drawing.Round(new RectangleF(1, 1, Width - 3, Height - 3), 10))
                using (var pen = new Pen(Theme.Accent, 2))
                    e.Graphics.DrawPath(pen, path);
        }
    }
}
