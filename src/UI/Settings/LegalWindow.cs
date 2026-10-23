using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class LegalWindow : PortableWindow
    {
        private readonly SettingsButton linkStatus = new SettingsButton { Text = "", Visible = false, TabStop = false, Bounds = new Rectangle(22, 552, 616, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        private readonly Label linkFeedback = new Label { Bounds = new Rectangle(22, 552, 616, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ForeColor = Theme.Muted };
        protected override bool CompactCaption
        {
            get
            {
                return true;
            }
        }
        internal LegalWindow()
        {
            Text = "Legal & Privacy";
            ClientSize = new Size(660, 600);
            MinimumSize = new Size(420, 320);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            var document = new SlimFlowPanel { Location = new Point(22, 52), Size = new Size(616, 488), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Theme.Background, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, AccessibleName = "Conditions d'utilisation, confidentialité et licences" };
            using (var stream = typeof(LegalWindow).Assembly.GetManifestResourceStream("legal.md"))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int level = line.StartsWith("### ") ? 3 : line.StartsWith("## ") ? 2 : line.StartsWith("# ") ? 1 : 0;
                    if (line.Length == 0)
                        continue;
                    var paragraph = new LinkLabel { Text = level > 0 ? line.Substring(level + 1) : line, AutoSize = true, MaximumSize = new Size(580, 0), Margin = new Padding(0, level > 0 ? 16 : 0, 0, 12), UseMnemonic = false, ForeColor = level > 0 ? Theme.AccentForeground : Theme.TextColor, LinkColor = Theme.AccentForeground, ActiveLinkColor = Theme.AccentForeground, VisitedLinkColor = Theme.AccentForeground, TabStop = false };
                    UiFonts.Assign(paragraph, UiFonts.Create("Segoe UI", level == 1 ? 18 : level == 2 ? 14 : level == 3 ? 11 : 10, level > 0 ? FontStyle.Bold : FontStyle.Regular));
                    paragraph.Links.Clear();
                    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(paragraph.Text, @"https://[^\s]+"))
                    {
                        paragraph.Links.Add(match.Index, match.Length, match.Value);
                        paragraph.TabStop = true;
                    }
                    paragraph.LinkClicked += (s, e) => OpenLink((string)e.Link.LinkData);
                    document.Controls.Add(paragraph);
                }
            }
            document.Resize += (s, e) =>
            {
                foreach (Control paragraph in document.Controls)
                    paragraph.MaximumSize = new Size(Math.Max(200, document.ClientSize.Width - 30), 0);
            };
            Controls.Add(document);
            Controls.Add(linkStatus);
            Controls.Add(linkFeedback);
        }
        private void OpenLink(string url)
        {
            if (linkStatus.Busy)
                return;
            linkFeedback.Hide();
            linkStatus.Visible = true;
            BusyOperation.OpenLink(this, linkStatus, url, error =>
            {
                linkStatus.Hide();
                linkFeedback.Text = error == null ? "Opened in your browser" : "Unable to open your browser. Select the link to retry.";
                linkFeedback.Show();
                if (error != null)
                    DiagnosticReport.Record(error);
            });
        }
    }
}
