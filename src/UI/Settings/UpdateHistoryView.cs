using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class UpdateHistoryView
    {
        internal static string Describe(ReleaseUpdate update)
        {
            var text = new StringBuilder();
            text.AppendLine("Installed: v" + ReleaseUpdates.CurrentVersion);
            text.AppendLine("Updating directly to " + update.Version + " includes the intermediate versions.");
            text.AppendLine();
            text.AppendLine(update.HistoryComplete ? update.Changes.Length + " newer versions:" : "Release history is incomplete or unavailable. You can still install the latest version.");
            foreach (var change in update.Changes)
            {
                text.AppendLine();
                text.AppendLine(change.Version + (change.Version == update.Version ? " — Latest" : " — Intermediate version"));
                string notes = Regex.Replace(change.Notes ?? "", @"<!--[\s\S]*?-->", "").Trim();
                notes = Regex.Replace(notes, @"(?m)^#{1,6}\s+", "");
                notes = Regex.Replace(notes, @"\[([^\]]+)\]\((https://[^\s)]+)\)", "$1 ($2)");
                text.AppendLine(notes.Length == 0 ? "No release notes were provided for this version." : notes);
            }
            return text.ToString();
        }
        internal static Control Create(ReleaseUpdate update)
        {
            var panel = new ReleaseNotesPanel { Bounds = new Rectangle(24, 136, 512, 342), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            foreach (string block in Describe(update).Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var label = new Label { Text = block.Trim(), AutoSize = true, MaximumSize = new Size(468, 0), ForeColor = Theme.TextColor, Margin = new Padding(0, 0, 0, 18), UseMnemonic = false, AccessibleRole = AccessibleRole.StaticText };
                panel.Controls.Add(label);
            }
            panel.Resize += (s, e) => { foreach (Control label in panel.Controls) label.MaximumSize = new Size(Math.Max(80, panel.ClientSize.Width - 40), 0); };
            return panel;
        }
        private sealed class ReleaseNotesPanel : SlimFlowPanel
        {
            internal ReleaseNotesPanel()
            {
                SetStyle(ControlStyles.Selectable, true);
                TabStop = true;
                AutoScroll = true;
                FlowDirection = FlowDirection.TopDown;
                WrapContents = false;
                Padding = new Padding(12);
                BackColor = Theme.Surface;
                AccessibleName = "Versions not yet installed and release notes";
                AccessibleDescription = "Use Up, Down, Page Up, Page Down, Home or End to scroll.";
            }
            protected override bool IsInputKey(Keys keyData)
            {
                return keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.PageUp || keyData == Keys.PageDown || keyData == Keys.Home || keyData == Keys.End || base.IsInputKey(keyData);
            }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                int y = -AutoScrollPosition.Y;
                if (e.KeyCode == Keys.Up)
                    y -= 24;
                else if (e.KeyCode == Keys.Down)
                    y += 24;
                else if (e.KeyCode == Keys.PageUp)
                    y -= ClientSize.Height;
                else if (e.KeyCode == Keys.PageDown)
                    y += ClientSize.Height;
                else if (e.KeyCode == Keys.Home)
                    y = 0;
                else if (e.KeyCode == Keys.End)
                    y = DisplayRectangle.Height;
                else
                {
                    base.OnKeyDown(e);
                    return;
                }
                AutoScrollPosition = new Point(0, Math.Max(0, y));
                e.Handled = true;
            }
            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                Invalidate();
            }
            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                Invalidate();
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Focused && ShowFocusCues)
                    ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(1, 1, Width - 3, Height - 3), Theme.AccentForeground, Theme.Surface);
            }
        }
    }
}
