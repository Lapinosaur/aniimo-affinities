using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class WindowAttentionTests
    {
        internal static void Run()
        {
            if (PortableWindow.AttentionOffset(0) != Point.Empty || PortableWindow.AttentionOffset(1) != Point.Empty || PortableWindow.AttentionOffset(0.05).Y == 0 || PortableWindow.AttentionOffset(0.05).X == 0)
                throw new Exception("Attention motion must use both axes and settle exactly");
            using (var owner = new PortableWindow())
            using (var dialog = new PortableWindow())
            using (var timer = new Timer { Interval = 30 })
            {
                owner.Show();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                int phase = 0;
                Point origin = Point.Empty;
                Exception failure = null;
                timer.Tick += (s, e) =>
                {
                    try
                    {
                        if (phase++ == 0)
                        {
                            origin = dialog.Location;
                            var message = Message.Create(owner.Handle, 0x20, owner.Handle, new IntPtr((0x201 << 16) | 0xfffe));
                            object[] parameters = { message };
                            typeof(PortableWindow).GetMethod("WndProc", flags).Invoke(owner, parameters);
                            var animation = (Timer)typeof(PortableWindow).GetField("attentionTimer", flags).GetValue(dialog);
                            if (!animation.Enabled)
                                throw new Exception("Blocked dialog did not animate");
                            for (int click = 0; click < 10; click++)
                            {
                                object[] repeat = { Message.Create(owner.Handle, 0x20, owner.Handle, new IntPtr((0x201 << 16) | 0xfffe)) };
                                typeof(PortableWindow).GetMethod("WndProc", flags).Invoke(owner, repeat);
                                if (((Message)repeat[0]).Result != new IntPtr(1))
                                    throw new Exception("Repeated blocked click was not silenced");
                            }
                            timer.Interval = 450;
                        }
                        else
                        {
                            if (dialog.Location != origin)
                                throw new Exception("Dialog shake did not restore position");
                            timer.Stop();
                            dialog.Close();
                        }
                    }
                    catch (Exception error) { failure = error; timer.Stop(); dialog.Close(); }
                };
                timer.Start();
                dialog.ShowDialog(owner);
                if (failure != null)
                    throw failure;
            }
        }
    }
}
