using System;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SupportShortcut : IMessageFilter
    {
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg != 0x100 || (Keys)message.WParam.ToInt32() != Keys.F11 || Control.ModifierKeys != Keys.None)
                return false;
            var control = Control.FromChildHandle(message.HWnd);
            var capture = control as KeyCapture;
            if (capture != null && capture.Listening)
                capture.CancelForSupport();
            if ((message.LParam.ToInt64() & (1L << 30)) == 0)
                SupportWindow.Open(control == null ? Form.ActiveForm : control.FindForm());
            return true;
        }
    }
}
