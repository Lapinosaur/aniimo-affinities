using System;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed partial class MainForm
    {
        private bool nativeClosePending;
        protected override void WndProc(ref Message message)
        {
            bool close = message.Msg == 0x10 || message.Msg == 0x112 && (message.WParam.ToInt64() & 0xfff0) == 0xf060;
            if (!childView && !TutorialSnapshot && HasModalChild(this) && close && !nativeClosePending)
            {
                nativeClosePending = true;
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!IsDisposed && CloseChildren(this))
                        {
                            Enabled = true;
                            Close();
                        }
                    }
                    finally { nativeClosePending = false; }
                }));
                message.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref message);
        }
        private static bool CloseChildren(Form owner)
        {
            foreach (var child in owner.OwnedForms)
            {
                if (!CloseChildren(child))
                    return false;
                child.Close();
                if (!child.IsDisposed && child.Visible && (!child.Modal || child.DialogResult == DialogResult.None))
                    return false;
            }
            return true;
        }
        private static bool HasModalChild(Form owner)
        {
            foreach (var child in owner.OwnedForms)
                if (child.Visible && (child.Modal || HasModalChild(child)))
                    return true;
            return false;
        }
    }
}
