using System;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class WindowZoomWheel : IMessageFilter
    {
        private IntPtr owner;
        private int remainder;
        private readonly Func<System.Drawing.Point, Control> hoveredControl;
        internal WindowZoomWheel(Func<System.Drawing.Point, Control> hoveredControl = null)
        {
            this.hoveredControl = hoveredControl ?? (point => Control.FromChildHandle(NativeMethods.WindowFromPoint(point)));
        }
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg != 0x020A)
                return false;
            var point = new System.Drawing.Point(unchecked((short)(message.LParam.ToInt64() & 0xffff)), unchecked((short)((message.LParam.ToInt64() >> 16) & 0xffff)));
            var control = hoveredControl(point) ?? Control.FromChildHandle(message.HWnd);
            var window = control == null ? Form.ActiveForm : control.FindForm();
            if (window is TipBubble)
                window = window.Owner ?? Form.ActiveForm;
            var portable = window as PortableWindow;
            bool supported = portable != null && (portable is MainForm || portable.IsContentWindow);
            if ((message.WParam.ToInt64() & 8) == 0 || !supported || Form.ActiveForm != null && Form.ActiveForm.Modal)
            {
                remainder = 0;
                return false;
            }
            HandleWheel(window, unchecked((short)((message.WParam.ToInt64() >> 16) & 0xffff)));
            return true;
        }
        internal void HandleWheel(Form window, int delta)
        {
            if (owner != window.Handle)
            {
                owner = window.Handle;
                remainder = 0;
            }
            remainder += delta;
            while (Math.Abs(remainder) >= 120)
            {
                int direction = Math.Sign(remainder);
                remainder -= direction * 120;
                WindowZoom.HandleShortcut(Keys.Control | (direction > 0 ? Keys.Up : Keys.Down), IntPtr.Zero, window as PortableWindow);
            }
        }
    }
}
