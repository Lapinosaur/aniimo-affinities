using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed class MarkdownNotesBox : RichTextBox
    {
        private readonly ApplicationTips linkTips = ApplicationTips.Create();
        private string hoveredUrl;
        private int linkStart, linkEnd;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int position = GetCharIndexFromPosition(e.Location);
            if (hoveredUrl != null && (position < linkStart || position >= linkEnd))
            {
                hoveredUrl = null;
                linkTips.SetToolTip(this, "");
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            hoveredUrl = null;
            linkTips.SetToolTip(this, "");
            base.OnMouseLeave(e);
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct LinkNotification
        {
            internal IntPtr Window, Id;
            internal int Code, Message;
            internal IntPtr WParam, LParam;
            internal int Start, End;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                linkTips.Dispose();
                Events.Dispose();
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            internal int Left, Top, Right, Bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct FormatRange
        {
            internal IntPtr Dc, TargetDc;
            internal Rect Area, Page;
            internal int Start, End;
        }
        // RichEdit does not paint its text through DrawToBitmap by default.
        // Use its own formatter for previews and the glass foreground capture.
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x204E && message.LParam != IntPtr.Zero
                && Marshal.ReadInt32(message.LParam, IntPtr.Size * 2) == 0x70B)
            {
                var notification = (LinkNotification)Marshal.PtrToStructure(message.LParam, typeof(LinkNotification));
                if (notification.Code == 0x70B && notification.Message == 0x200
                    && notification.Start >= 0 && notification.End <= TextLength && notification.End > notification.Start)
                {
                    string url = Text.Substring(notification.Start, notification.End - notification.Start);
                    if (url != hoveredUrl)
                    {
                        hoveredUrl = url;
                        linkStart = notification.Start;
                        linkEnd = notification.End;
                        linkTips.SetToolTip(this, ExternalLinkTips.Describe(url));
                        linkTips.QueueTip(this);
                    }
                }
            }
            if ((message.Msg == 0x317 || message.Msg == 0x318) && message.WParam != IntPtr.Zero)
            {
                using (var graphics = Graphics.FromHdc(message.WParam))
                {
                    graphics.Clear(BackColor);
                    var area = new Rect { Right = (int)(ClientSize.Width * 1440f / graphics.DpiX), Bottom = (int)(ClientSize.Height * 1440f / graphics.DpiY) };
                    var range = new FormatRange { Dc = message.WParam, TargetDc = message.WParam, Area = area, Page = area, Start = 0, End = -1 };
                    IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FormatRange)));
                    try
                    {
                        Marshal.StructureToPtr(range, memory, false);
                        NativeMethods.SendMessage(Handle, 0x439, new IntPtr(1), memory);
                    }
                    finally
                    {
                        NativeMethods.SendMessage(Handle, 0x439, IntPtr.Zero, IntPtr.Zero);
                        Marshal.FreeHGlobal(memory);
                    }
                }
                message.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref message);
        }
    }
}
