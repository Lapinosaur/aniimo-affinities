using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AniimoAffinities
{
    internal static class GlassNative
    {
        internal static bool RoundedCorners
        {
            get; private set;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowPosition
        {
            internal IntPtr Window, After;
            internal int X, Y, Width, Height;
            internal uint Flags;
        }
        internal static bool ZOrderChanged(IntPtr data)
        {
            var position = (WindowPosition)Marshal.PtrToStructure(data, typeof(WindowPosition));
            return (position.Flags & 4) == 0;
        }
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr BeginDeferWindowPos(int count);
        [DllImport("user32.dll")] private static extern IntPtr DeferWindowPos(IntPtr batch, IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern bool EndDeferWindowPos(IntPtr batch);
        internal static void PositionTogether(IntPtr[] windows, Rectangle[] bounds, bool show)
        {
            IntPtr batch = BeginDeferWindowPos(windows.Length);
            for (int i = 0; i < windows.Length && batch != IntPtr.Zero; i++)
            {
                var rectangle = bounds[i];
                batch = DeferWindowPos(batch, windows[i], IntPtr.Zero, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, (uint)(0x14 | (show ? 0x40 : 0)));
            }
            if (batch != IntPtr.Zero && EndDeferWindowPos(batch))
                return;
            for (int i = 0; i < windows.Length; i++)
                Position(windows[i], bounds[i], show);
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            internal int Left, Top, Right, Bottom;
        }
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);
        internal static Rectangle WindowBounds(IntPtr window)
        {
            NativeRect rectangle;
            GetWindowRect(window, out rectangle);
            return Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }
        internal static void Position(IntPtr window, Rectangle bounds, bool show)
        {
            SetWindowPos(window, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height, (uint)(0x14 | (show ? 0x40 : 0)));
        }
        internal static void Hide(IntPtr window)
        {
            SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, 0x97);
        }
        internal static void Raise(IntPtr window)
        {
            SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, 0x13);
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Accent
        {
            internal int State, Flags, Color, Animation;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Attribute
        {
            internal int Id; internal IntPtr Data; internal int Size;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Blend
        {
            internal byte Operation, Flags, Alpha, Format;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            internal int HeaderSize, Width, Height;
            internal short Planes, Bits;
            internal int Compression, ImageSize, XResolution, YResolution, Colors, Important;
        }
        [DllImport("gdi32.dll")] private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo info, uint usage, out IntPtr pixels, IntPtr section, uint offset);
        [DllImport("user32.dll")] private static extern bool SetWindowCompositionAttribute(IntPtr window, ref Attribute attribute);
        [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr window, int key, byte alpha, int flags);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr item);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr item);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screen, ref Point position, ref Size size, IntPtr source, ref Point origin, int key, ref Blend blend, int flags);

        // The accent API is capability-probed: Windows 10/11 support blur-behind.
        // Unsupported composition keeps the ordinary opaque window intact.
        internal static bool Enable(IntPtr window, Color tint)
        {
            var policy = new Accent { State = 4, Color = (35 << 24) | (tint.B << 16) | (tint.G << 8) | tint.R };
            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(policy));
            try
            {
                Marshal.StructureToPtr(policy, memory, false);
                var attribute = new Attribute { Id = 19, Data = memory, Size = Marshal.SizeOf(policy) };
                int corners = 2;
                RoundedCorners = DwmSetWindowAttribute(window, 33, ref corners, sizeof(int)) == 0;
                int noBorder = -2;
                if (DwmSetWindowAttribute(window, 34, ref noBorder, sizeof(int)) != 0)
                    System.Diagnostics.Debug.WriteLine("Native border styling unavailable.");
                if (!SetLayeredWindowAttributes(window, 0, 255, 2))
                    return false;
                return SetWindowCompositionAttribute(window, ref attribute);
            }
            catch (EntryPointNotFoundException) { return false; }
            catch (DllNotFoundException) { return false; }
            finally { Marshal.FreeHGlobal(memory); }
        }

        internal static bool Present(IntPtr window, Bitmap image, Point position)
        {
            IntPtr screen = GetDC(IntPtr.Zero);
            if (screen == IntPtr.Zero)
                return false;
            IntPtr source = CreateCompatibleDC(screen);
            if (source == IntPtr.Zero)
            {
                ReleaseScreen(screen);
                return false;
            }
            var info = new BitmapInfo { HeaderSize = Marshal.SizeOf(typeof(BitmapInfo)), Width = image.Width, Height = -image.Height, Planes = 1, Bits = 32 };
            IntPtr pixels;
            IntPtr bitmap = CreateDIBSection(screen, ref info, 0, out pixels, IntPtr.Zero, 0);
            if (bitmap == IntPtr.Zero)
            {
                DeleteDC(source);
                ReleaseScreen(screen);
                return false;
            }
            IntPtr previous = SelectObject(source, bitmap);
            try
            {
                var data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var row = new int[image.Width];
                    for (int y = 0; y < image.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                        Marshal.Copy(row, 0, IntPtr.Add(pixels, y * image.Width * 4), row.Length);
                    }
                }
                finally { image.UnlockBits(data); }
                var size = image.Size;
                var origin = Point.Empty;
                var blend = new Blend { Alpha = 255, Format = 1 };
                return UpdateLayeredWindow(window, screen, ref position, ref size, source, ref origin, 0, ref blend, 2);
            }
            finally
            {
                SelectObject(source, previous);
                DeleteObject(bitmap);
                DeleteDC(source);
                ReleaseScreen(screen);
            }
        }
        private static void ReleaseScreen(IntPtr screen)
        {
            if (ReleaseDC(IntPtr.Zero, screen) == 0)
                System.Diagnostics.Debug.WriteLine("Could not release the glass display DC.");
        }
    }
}
