using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class SettingsDialog
    {
        private void AddTutorialChoices(Panel panel, Action startTutorial)
        {
            panel.AutoScroll = true;
            AddTechnicalHelp(panel);
            foreach (Control control in panel.Controls)
                control.Top += 170;
            panel.Height = 560;
            panel.Controls.Add(new Label { Text = "Choose the sections to replay. Paused tours resume where you left off.", ForeColor = Theme.Muted, Bounds = new Rectangle(0, 4, 392, 36) });
            var progress = TutorialProgress.Load();
            var choices = new CheckBox[TutorialOverlay.Sections.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i] = new RoundCheckBox { Text = TutorialOverlay.Sections[i], Checked = (progress.Sections & (1 << i)) != 0, ForeColor = Theme.TextColor, Bounds = new Rectangle(0, 44 + i * 28, 392, 26), TabIndex = i };
                panel.Controls.Add(choices[i]);
            }
            var hide = new RoundCheckBox { Text = "Don't show at startup", Checked = TutorialPreferences.Hidden, ForeColor = Theme.TextColor, Bounds = new Rectangle(0, 220, 392, 26), TabIndex = 6 };
            var status = new Label { ForeColor = Theme.Muted, Bounds = new Rectangle(0, 250, 392, 34) };
            var resume = new SettingsButton { Text = "Resume", Bounds = new Rectangle(0, 292, 122, 34), TabIndex = 7 };
            var replay = new SettingsButton { Text = "Replay selected", Primary = true, Bounds = new Rectangle(134, 292, 258, 34), TabIndex = 8 };
            Action refresh = () =>
            {
                progress = TutorialProgress.Load();
                resume.Enabled = progress.Pending && startTutorial != null;
                replay.Enabled = choices.Any(choice => choice.Checked) && startTutorial != null;
                status.Text = progress.Pending ? "Paused at step " + (progress.Step + 1) + ". Resume keeps the previous section selection." : "Select one or more sections, then replay.";
                hide.Checked = TutorialPreferences.Hidden;
            };
            Action<bool> launch = restart =>
            {
                try
                {
                    if (restart)
                    {
                        int mask = 0;
                        for (int i = 0; i < choices.Length; i++)
                            if (choices[i].Checked)
                                mask |= 1 << i;
                        if (mask == 0)
                            return;
                        new TutorialProgress { Sections = mask }.Save();
                    }
                    if (startTutorial != null)
                        startTutorial();
                    refresh();
                }
                catch (IOException) { status.Text = "Unable to save tutorial progress. Please try again."; }
                catch (UnauthorizedAccessException) { status.Text = "Unable to save tutorial progress: access denied."; }
            };
            resume.Click += (s, e) => launch(false);
            replay.Click += (s, e) => launch(true);
            foreach (var choice in choices)
                choice.CheckedChanged += (s, e) => { replay.Enabled = choices.Any(item => item.Checked) && startTutorial != null; };
            hide.CheckedChanged += (s, e) =>
            {
                try
                {
                    TutorialPreferences.Save(hide.Checked);
                }
                catch (IOException) { status.Text = "Unable to save startup preference."; }
                catch (UnauthorizedAccessException) { status.Text = "Unable to save startup preference."; }
            };
            panel.Controls.AddRange(new Control[] { hide, status, resume, replay });
            panel.VisibleChanged += (s, e) => { if (panel.Visible) refresh(); };
            refresh();
        }
    }
}
