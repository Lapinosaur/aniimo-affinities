using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class LifetimeTests
    {
        // Model a client retaining an accessibility object/control after its window
        // closes. Its callbacks must no longer retain the former screen's state.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference Connect(Control control)
        {
            var screen = new object();
            control.Click += (s, e) => GC.KeepAlive(screen);
            var button = control as SettingsButton;
            if (button != null)
            {
                button.SurroundColor = () => { GC.KeepAlive(screen); return Color.Black; };
                button.PaintSurround = graphics => GC.KeepAlive(screen);
            }
            var panel = control as DrawingPanel;
            if (panel != null)
                panel.AccessibleText = () => { GC.KeepAlive(screen); return "Description"; };
            var check = control as RoundCheckBox;
            if (check != null)
                check.SurfaceColor = () => { GC.KeepAlive(screen); return Color.Black; };
            var hold = control as HoldSizeButton;
            if (hold != null)
                hold.Commit += value => GC.KeepAlive(screen);
            var scroll = control as SlimScrollBar;
            if (scroll != null)
                scroll.ValueChanged += value => GC.KeepAlive(screen);
            var bubble = control as TipBubble;
            if (bubble != null)
            {
                bubble.SetApplyAction(() => GC.KeepAlive(screen));
                bubble.SetPreviewAction(() => GC.KeepAlive(screen));
                bubble.SetOpenAction(() => GC.KeepAlive(screen));
                bubble.SetDismissAction(() => GC.KeepAlive(screen));
            }
            return new WeakReference(screen);
        }
        internal static void Run()
        {
            Control[] controls = { new SettingsButton(), new DrawingPanel(), new RoundCheckBox(), new HoldSizeButton(), new SlimScrollBar(), new SlimScrollPanel(), new SlimFlowPanel(), new MarkdownNotesBox(), new NameInput("", "", "Name"), new ThemePreview(), new ContentWindowPreview(), new LanguagePicker(), new TipBubble() };
            foreach (var control in controls)
            {
                var screen = Connect(control);
                bool disposed = false;
                control.Disposed += (s, e) => disposed = true;
                var accessibility = control.AccessibilityObject;
                control.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (!disposed || screen.IsAlive)
                    throw new Exception(control.GetType().Name + " must raise Disposed and release screen callbacks even when its accessibility object is retained.");
                GC.KeepAlive(accessibility);
                GC.KeepAlive(control);
            }
        }
    }
}
