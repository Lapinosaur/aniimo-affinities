using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class SettingsDialog
    {
        private Action fitHelpHeight;
        private void AddTutorialChoices(Panel panel, Action startTutorial)
        {
            panel.Height = 548;
            var tutorials = new SettingsButton { Text = Language.T("Tutorials"), Primary = true, Bounds = new Rectangle(0, 0, 146, 34), TabIndex = 0 };
            var support = new SettingsButton { Text = Language.T("Support"), Bounds = new Rectangle(146, 0, 146, 34), TabIndex = 1 };
            SettingsButton.Group(tutorials, support);
            var list = new SlimScrollPanel { AutoScroll = true, Bounds = new Rectangle(0, 46, 412, 502), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, TabIndex = 2 };
            var help = new SlimScrollPanel { Bounds = list.Bounds, Anchor = list.Anchor, Visible = false, AutoScroll = true, TabIndex = 3 };
            AddTechnicalHelp(help);
            help.Bounds = list.Bounds;
            int supportContentHeight = help.Controls.Cast<Control>().Max(control => control.Bottom);
            fitHelpHeight = () =>
            {
                int desired = support.Primary ? panel.Top + help.Top + supportContentHeight + 20 : 720;
                var workArea = Screen.FromControl(this).WorkingArea;
                int target = Math.Min(desired, workArea.Height - 32);
                if (Top + target > workArea.Bottom)
                    Top = Math.Max(workArea.Top, workArea.Bottom - target);
                TransitionHeight(target);
            };
            panel.Controls.AddRange(new Control[] { tutorials, support, list, help });
            tutorials.Click += (s, e) => { tutorials.Primary = true; support.Primary = false; list.Show(); help.Hide(); tutorials.Invalidate(); support.Invalidate(); fitHelpHeight(); };
            support.Click += (s, e) => { tutorials.Primary = false; support.Primary = true; list.Hide(); help.Show(); tutorials.Invalidate(); support.Invalidate(); fitHelpHeight(); };
            var summary = new Label { ForeColor = Theme.TextColor, Bounds = new Rectangle(0, 0, 390, 24) };
            list.Controls.Add(summary);
            var playAll = new SettingsButton { Text = Language.T("Play all"), AccessibleName = Language.T("Play all tutorials"), Primary = true, Bounds = new Rectangle(0, 28, 125, 34), Enabled = startTutorial != null, TabIndex = 0 };
            var remaining = new SettingsButton { Text = Language.T("Play unfinished"), AccessibleName = Language.T("Play unfinished tutorials"), Bounds = new Rectangle(133, 28, 148, 34), TabIndex = 1 };
            list.Controls.AddRange(new Control[] { playAll, remaining });
            var checks = new RoundCheckBox[TutorialOverlay.Sections.Length];
            var actions = new SettingsButton[TutorialOverlay.Sections.Length];
            string[] purposes = { Language.T("Select types and read damage multipliers."), Language.T("Compare attacking rows and defending columns."), Language.T("Read radars, check allies and highlight their traces."), Language.T("Keep the app above a game and lock the overlay."), Language.T("Assign, test and resolve shortcut conflicts."), Language.T("Adjust theme, text and window size safely."), Language.T("Suggestions, projected teams and ally radar traces."), Language.T("Discover new suggestions, reports and comparisons."), Language.T("Coverage, Counter, radar preview and applying a proposal."), Language.T("Check, read release notes, postpone and install updates.") };
            var status = new Label { ForeColor = Theme.Muted, Bounds = new Rectangle(0, 756, 390, 44) };
            var resume = new SettingsButton { Text = Language.T("Resume"), AccessibleName = Language.T("Resume paused tour"), Bounds = new Rectangle(289, 28, 101, 34), TabIndex = 2 };
            list.Controls.Add(resume);
            Action refresh = () =>
            {
                var progress = TutorialProgress.Load();
                summary.Text = Enumerable.Range(0, TutorialOverlay.Sections.Length).Count(i => (progress.Completed & (1 << i)) != 0) + Language.T(" of ") + TutorialOverlay.Sections.Length + Language.T(" tutorials completed");
                resume.Enabled = progress.Pending && startTutorial != null;
                resume.Visible = progress.Pending;
                remaining.Enabled = startTutorial != null && (TutorialProgress.All & ~progress.Completed) != 0;
                for (int i = 0; i < TutorialOverlay.Sections.Length; i++)
                {
                    if (checks[i] == null)
                        continue;
                    bool completed = (progress.Completed & (1 << i)) != 0;
                    bool started = (progress.Started & (1 << i)) != 0;
                    checks[i].Checked = completed;
                    checks[i].AccessibleDescription = completed ? Language.T("Completed tutorial") : started ? Language.T("Tutorial in progress") : Language.T("Tutorial not started");
                    actions[i].Text = completed ? Language.T("Replay") : started ? Language.T("Resume") : Language.T("Play");
                    actions[i].AccessibleName = actions[i].Text + " " + TutorialOverlay.Sections[i];
                }
            };
            Action<int> launch = section =>
            {
                try
                {
                    if (section >= 0)
                    {
                        var progress = TutorialProgress.Load();
                        progress.Sections = 1 << section;
                        progress.Pending = (progress.Started & (1 << section)) != 0 && (progress.Completed & (1 << section)) == 0;
                        progress.Step = progress.Pending ? progress.SectionSteps[section] : 0;
                        progress.Save();
                    }
                    if (startTutorial != null)
                        startTutorial();
                    refresh();
                }
                catch (IOException) { status.Text = Language.T("Unable to save tutorial progress. Please try again."); }
                catch (UnauthorizedAccessException) { status.Text = Language.T("Unable to save tutorial progress: access denied."); }
            };
            resume.Click += (s, e) => launch(-1);
            Action<bool> launchBatch = unfinished =>
            {
                try
                {
                    var progress = TutorialProgress.Load();
                    int sections = unfinished ? TutorialProgress.All & ~progress.Completed : TutorialProgress.All;
                    if (sections == 0 || startTutorial == null)
                        return;
                    progress.Sections = sections;
                    progress.Step = 0;
                    progress.Pending = false;
                    progress.Save();
                    startTutorial();
                    refresh();
                }
                catch (IOException) { status.Text = Language.T("Unable to save tutorial progress. Please try again."); }
                catch (UnauthorizedAccessException) { status.Text = Language.T("Unable to save tutorial progress: access denied."); }
            };
            playAll.Click += (s, e) => launchBatch(false);
            remaining.Click += (s, e) => launchBatch(true);
            int y = 76;
            int[] order = { 7, 0, 6, 8, 1, 2, 3, 4, 5, 9 };
            for (int row = 0; row < order.Length; row++)
            {
                int section = order[row];
                if (row == 0 || row == 1 || row == 6)
                {
                    list.Controls.Add(UiFonts.Own(new Label { Text = row == 0 ? Language.T("New features") : row == 1 ? Language.T("Understand matchups") : Language.T("Your workspace"), ForeColor = Theme.TextColor, Font = UiFonts.Create("Segoe UI", 11, FontStyle.Bold), Bounds = new Rectangle(0, y, 390, 28) }));
                    y += 32;
                }
                checks[section] = new RoundCheckBox { Text = TutorialOverlay.Sections[section], AutoCheck = false, TabStop = false, AccessibleDescription = Language.T("Completion status, updated when the tutorial is finished."), ForeColor = Theme.TextColor, Bounds = new Rectangle(0, y, 278, 26) };
                var purpose = new Label { Text = purposes[section], ForeColor = Theme.Muted, Bounds = new Rectangle(29, y + 28, 352, 30) };
                actions[section] = new SettingsButton { Bounds = new Rectangle(280, y, 110, 26), Enabled = startTutorial != null, TabIndex = row + 3 };
                actions[section].Click += (s, e) => launch(section);
                list.Controls.AddRange(new Control[] { checks[section], purpose, actions[section] });
                y += 64;
            }
            var startup = new RoundCheckBox { Text = Language.T("Show unfinished tutorials at startup"), Checked = !TutorialPreferences.Hidden, ForeColor = Theme.TextColor, Bounds = new Rectangle(0, y + 12, 390, 30), TabIndex = 12 };
            startup.CheckedChanged += (s, e) =>
            {
                try
                {
                    TutorialPreferences.Save(!startup.Checked);
                }
                catch (IOException) { status.Text = Language.T("Unable to save startup preference."); }
                catch (UnauthorizedAccessException) { status.Text = Language.T("Unable to save startup preference: access denied."); }
            };
            status.Top = startup.Bottom + 8;
            list.Controls.AddRange(new Control[] { startup, status });
            panel.VisibleChanged += (s, e) => { if (panel.Visible) { refresh(); startup.Checked = !TutorialPreferences.Hidden; } };
            refresh();
        }
    }
}
