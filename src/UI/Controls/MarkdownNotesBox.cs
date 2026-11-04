using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal sealed class MarkdownNotesBox : RichTextBox
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Events.Dispose();
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
