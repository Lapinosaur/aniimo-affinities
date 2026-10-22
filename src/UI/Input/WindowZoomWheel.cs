using System;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class WindowZoomWheel : IMessageFilter
    {
        private IntPtr owner;
        private int remainder;
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg != 0x020A)
                return false;
            var control = Control.FromChildHandle(message.HWnd);
            var window = control == null ? null : control.FindForm();
            bool supported = window is MainForm || window is RadarDialog || window is MatrixDialog;
            if ((message.WParam.ToInt64() & 8) == 0 || !supported || Form.ActiveForm != window)
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
                WindowZoom.HandleShortcut(Keys.Control | (direction > 0 ? Keys.Up : Keys.Down), IntPtr.Zero);
            }
        }
    }
}
