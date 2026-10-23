using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace AniimoAffinities
{
    internal static class BubbleClickTests
    {
        internal static void Run()
        {
            Point original = Cursor.Position;
            try
            {
                using (var host = new Form { TopMost = true, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(80, 80, 480, 450) })
                using (var anchor = new Button { Text = "Suggestion", Bounds = new Rectangle(100, 80, 180, 50) })
                using (var tips = ApplicationTips.Create())
                {
                    host.Controls.Add(anchor);
                    host.Show();
                    host.Activate();
                    tips.SetHoverBubble(anchor, true);
                    tips.SetScrollableGraphic(anchor, true);
                    tips.SetGraphicTip(anchor, "All matchups", new Size(250, 2460), (g, r) => { });
                    tips.ShowTip(anchor);
                    var scrollBubble = (TipBubble)typeof(ApplicationTips).GetField("bubble", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tips);
                    var scroll = scrollBubble.Controls.OfType<SlimScrollPanel>().Single();
                    scroll.AutoScrollPosition = new Point(0, 2400);
                    if (!scroll.Visible || scrollBubble.Height > 540 || scroll.AutoScrollPosition.Y >= -1800 || scrollBubble.Remaining != 0)
                        throw new Exception("Matchup bubble must reach its final entries without a timed cooldown.");
                    scrollBubble.Controls.OfType<SettingsButton>().Single(item => item.AccessibleName == "Close bubble").PerformClick();
                    tips.SetScrollableGraphic(anchor, false);
                    tips.SetGraphicTip(anchor, "Suggested coverage", new Size(350, 150), (g, r) => { });
                    tips.SetHoverBubble(anchor, true);
                    int applied = 0, previewed = 0, opened = 0;
                    tips.SetApplyAction(anchor, () => applied++);
                    tips.SetPreviewAction(anchor, () => previewed++);
                    tips.SetOpenAction(anchor, () => opened++);
                    string[] actions = { "Preview suggestion on radars", "Apply suggestion", "Open all suggestions", "Close bubble" };
                    for (int i = 0; i < actions.Length; i++)
                    {
                        Cursor.Position = anchor.PointToScreen(new Point(20, 20));
                        tips.ShowTip(anchor);
                        var bubble = (TipBubble)typeof(ApplicationTips).GetField("bubble", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tips);
                        var button = bubble.Controls.OfType<SettingsButton>().Single(item => item.AccessibleName == actions[i]);
                        var center = new Point(button.Width / 2, button.Height / 2);
                        var coordinates = new IntPtr((center.Y << 16) | center.X);
                        // Dispatch the native activation/down/up sequence to the target HWND.
                        // Global mouse injection is unreliable while another app moves the pointer.
                        var activation = NativeMethods.SendMessage(bubble.Handle, 0x21, host.Handle, new IntPtr((0x201 << 16) | 1));
                        if (activation != new IntPtr(3))
                            throw new Exception("Bubble mouse activation must deliver the click without activating the popup.");
                        NativeMethods.SendMessage(button.Handle, 0x201, new IntPtr(1), coordinates);
                        if (!bubble.Visible)
                            throw new Exception("Bubble disappeared on mouse-down before " + actions[i] + " could run.");
                        Cursor.Position = button.PointToScreen(center);
                        NativeMethods.SendMessage(button.Handle, 0x202, IntPtr.Zero, coordinates);
                        Pump();
                        if (bubble.Visible || previewed != 1 || applied != (i >= 1 ? 1 : 0) || opened != (i >= 2 ? 1 : 0))
                            throw new Exception("Native click must invoke exactly its intended action once.");
                    }
                }
            }
            finally { Cursor.Position = original; }
        }
        private static void Pump()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 50)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(2);
            }
        }
    }
}
