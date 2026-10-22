using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class LegalWindow : PortableWindow
    {
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
            var document = new FlowLayoutPanel { Location = new Point(22, 52), Size = new Size(616, 524), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Theme.Background, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, AccessibleName = "Conditions d'utilisation, confidentialité et licences" };
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
        }
        private void OpenLink(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception) { MessageBox.Show(this, "Unable to open your browser."); }
        }
    }
}
