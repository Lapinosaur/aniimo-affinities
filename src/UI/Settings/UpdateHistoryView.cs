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
            text.AppendLine(Language.T("Installed: v") + ReleaseUpdates.CurrentVersion);
            text.AppendLine(Language.T("Updating directly to ") + update.Version + Language.T(" includes the intermediate versions."));
            text.AppendLine();
            text.AppendLine(update.HistoryComplete ? update.Changes.Length + Language.T(" newer versions:") : Language.T("Release history is incomplete or unavailable. You can still install the latest version."));
            foreach (var change in update.Changes)
            {
                text.AppendLine();
                text.AppendLine("# " + change.Version + (change.Version == update.Version ? Language.T(" — Latest") : Language.T(" — Intermediate version")));
                string notes = Regex.Replace(change.Notes ?? "", @"<!--[\s\S]*?-->", "").Trim();
                text.AppendLine(notes.Length == 0 ? Language.T("No release notes were provided for this version.") : notes);
            }
            return text.ToString();
        }
        internal static Control Create(ReleaseUpdate update)
        {
            var panel = new ReleaseNotesPanel { Bounds = new Rectangle(24, 136, 512, 342), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            var notes = new MarkdownNotesBox { ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.None, WordWrap = true, DetectUrls = true, BackColor = Theme.Surface, ForeColor = Theme.TextColor, Width = 468, Height = 100, Margin = Padding.Empty, AccessibleName = Language.T("Formatted release notes") };
            notes.ContentsResized += (s, e) => notes.Height = Math.Max(40, e.NewRectangle.Height + 12);
            notes.LinkClicked += (s, e) =>
            {
                Uri uri;
                if (!Uri.TryCreate(e.LinkText, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                    return;
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception error) { DiagnosticReport.Record(error); }
            };
            panel.Controls.Add(notes);
            Action layout = () =>
            {
                int width = Math.Max(80, panel.ClientSize.Width - 40);
                if (notes.Width == width && notes.TextLength > 0)
                    return;
                notes.Width = width;
                notes.Rtf = MarkdownRtf.Render(Describe(update), width);
            };
            panel.Resize += (s, e) => layout();
            layout();
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
                AccessibleName = Language.T("Versions not yet installed and release notes");
                AccessibleDescription = Language.T("Use Up, Down, Page Up, Page Down, Home or End to scroll.");
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
