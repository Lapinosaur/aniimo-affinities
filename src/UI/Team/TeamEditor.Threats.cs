using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class TeamEditor
    {
        private ThreatReportPanel threatReport;
        private SettingsButton threatToggle;
        private Control[] footer;
        private void InitializeThreatReport()
        {
            footer = Controls.Cast<Control>().Where(control => control.Top >= 414).ToArray();
            foreach (var control in footer)
                control.Top += 72;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + 72);
            threatToggle = new ThreatReportHeader { Text = "Threat report ▾", AccessibleName = "Expand threat report and vulnerable allies", Location = new Point(32, 410), Size = new Size(618, 64) };
            threatReport = new ThreatReportPanel
            {
                Location = new Point(32, 482),
                Size = new Size(618, 280),
                Visible = false,
                AccessibleName = "Team threats and most exposed allies",
                TabIndex = 21
            };
            threatToggle.TabIndex = 20;
            threatToggle.Click += (s, e) =>
            {
                bool expanded = !threatReport.Visible;
                using (new VisualUpdate(this))
                {
                    int delta = expanded ? 290 : -290;
                    threatReport.Visible = expanded;
                    threatToggle.Text = expanded ? "Threat report ▴" : "Threat report ▾";
                    threatToggle.AccessibleName = expanded ? "Collapse threat report and vulnerable allies" : "Expand threat report and vulnerable allies";
                    foreach (var control in footer)
                        control.Top += delta;
                    ClientSize = new Size(ClientSize.Width, ClientSize.Height + delta);
                    RefreshThreatReport();
                }
            };
            Controls.Add(threatToggle);
            Controls.Add(threatReport);
        }
        private void RefreshThreatReport()
        {
            if (threatReport != null && threatReport.Visible)
            {
                threatReport.AccessibleName = (enemy ? "Known opponents" : "Your team") + " threats and most exposed members";
                threatReport.UpdateReport(plan, enemy);
            }
        }
    }
}
