using System;
using System.Drawing;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class TeamPreview
    {
        internal static void Save(string mode, string path)
        {
            if (mode == "team-zoom-lock" || mode == "team-zoom-normal")
            {
                using (var main = new MainForm())
                {
                    main.StartPosition = FormStartPosition.Manual;
                    main.Location = new Point(20, 20);
                    main.Show();
                    var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    var team = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(main);
                    team.Visible = true;
                    team.Position = "Bottom";
                    team.Allies[0].Main = 0;
                    typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(main, null);
                    main.FinishTeamTransition();
                    if (mode == "team-zoom-lock")
                        typeof(PortableWindow).GetMethod("SetOverlayLocked", flags).Invoke(main, new object[] { true });
                    foreach (float scale in new[] { .5f, 1f, 1.5f, 1.25f, 1.5f })
                    {
                        DisplaySettings.ApplyDisplay(DisplaySettings.FontScale, DisplaySettings.ShowBadges, scale);
                        Application.DoEvents();
                    }
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    while (clock.ElapsedMilliseconds < 300)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(10);
                    }
                    using (var image = new Bitmap(main.Width, main.Height))
                    {
                        main.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        image.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-suggestion-help")
            {
                using (var owner = new Form())
                using (var anchor = new Button())
                using (var bubble = new TipBubble())
                {
                    owner.Controls.Add(anchor);
                    owner.Show();
                    bubble.Present(anchor, "Suggestion help", null, null, SuggestionHelpGraphic.Draw, SuggestionHelpGraphic.Size);
                    using (var bitmap = new Bitmap(bubble.Width, bubble.Height))
                    {
                        bubble.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                        bitmap.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-glass-main" || mode == "team-glass-settings")
            {
                InteractiveGlassPreview.Save(mode, path);
                return;
            }
            if (mode == "team-suggestions-window")
            {
                var sample = new TeamPlan();
                sample.Allies[0].Main = 7;
                sample.Allies[0].Sub = 4;
                sample.Allies[1].Main = 2;
                sample.Allies[1].Sub = 8;
                sample.Allies[2].Main = 0;
                sample.Allies[2].Sub = 1;
                using (var window = new SuggestionsDialog(sample, 4, (slot, member) => false))
                {
                    window.Show();
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(window.Width, window.Height))
                    {
                        window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                        bitmap.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-suggestion-radar" || mode == "team-suggestion-radar-hover")
            {
                var previewPlan = TeamTests.Sample();
                previewPlan.Allies[5].Main = previewPlan.Allies[5].Sub = -1;
                var suggestion = TeamSuggestions.ComplementaryAdditions(previewPlan)[5];
                using (var dialog = new SuggestionRadarDialog(previewPlan, 5, suggestion, new[] { 0, 1, 2 }))
                {
                    if (mode == "team-suggestion-radar-hover")
                        dialog.HighlightedSlot = 5;
                    dialog.Show();
                    Application.DoEvents();
                    using (var image = new Bitmap(dialog.Width, dialog.Height))
                    {
                        dialog.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        image.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-suggestion-tip" || mode == "team-counter-tip")
            {
                var suggestionPlan = new TeamPlan();
                if (mode == "team-counter-tip")
                {
                    suggestionPlan.Opponents[0].Main = 0;
                    suggestionPlan.Opponents[0].Sub = 5;
                    suggestionPlan.Opponents[1].Main = 7;
                }
                var recommendation = TeamSuggestions.ComplementaryAdditions(suggestionPlan)[mode == "team-counter-tip" ? 0 : 2];
                using (var owner = new Form())
                using (var anchor = new Button { Text = "+" })
                using (var bubble = new TipBubble())
                {
                    owner.Controls.Add(anchor);
                    owner.Show();
                    bubble.SetOpenAction(() => { });
                    bubble.SetDismissAction(() => bubble.Hide());
                    bubble.SetApplyAction(() => bubble.Hide());
                    bubble.SetPreviewAction(() => bubble.Hide());
                    bubble.Present(anchor, TeamTypeSelector.RecommendationTip(recommendation), null, null,
                        (g, area) => SuggestionGraphic.Draw(g, area, recommendation, "Apply with + or Ctrl+Enter"), SuggestionGraphic.Size(recommendation));
                    using (var image = new Bitmap(bubble.Width, bubble.Height))
                    {
                        bubble.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        image.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-legal")
            {
                using (var window = new LegalWindow())
                {
                    window.Show();
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(window.Width, window.Height))
                    {
                        window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
                        bitmap.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-support-layout")
            {
                var layoutPlan = TeamTests.Sample();
                layoutPlan.Position = "Right";
                using (var host = new Form { ClientSize = new Size(271, 540), BackColor = Theme.Background })
                using (var board = new TeamBoard(layoutPlan, index => { }) { Location = new Point(55, 50), Size = new Size(216, 490) })
                using (var support = new SupportButton { Size = new Size(154, 48) })
                using (var bubble = new TipBubble())
                {
                    host.Controls.Add(board);
                    host.Controls.Add(support);
                    host.Show();
                    bubble.Present(support, "Support Lapinosaur\nScan with your phone", null, support.QrCode);
                    bubble.Relocate(new Rectangle(new Point(board.PointToScreen(Point.Empty).X - 55, bubble.Top), bubble.Size));
                    board.AvoidSupportBubble(bubble.Bounds);
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
                    {
                        board.DrawToBitmap(bitmap, board.Bounds);
                        support.DrawToBitmap(bitmap, support.Bounds);
                        bubble.DrawToBitmap(bitmap, host.RectangleToClient(bubble.Bounds));
                        bitmap.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-support-tooltip-side" || mode == "team-support-tooltip" || mode == "team-tooltip" || mode == "team-card-tooltip")
            {
                using (var owner = new Form())
                using (var card = new TeamCard(TeamTests.Sample(), 0))
                using (var bubble = new TipBubble())
                using (var support = new SupportButton())
                {
                    owner.Controls.Add(card);
                    owner.Show();
                    Application.DoEvents();
                    bubble.Remaining = mode.StartsWith("team-support-tooltip") ? 0 : .65f;
                    if (mode.StartsWith("team-support-tooltip"))
                        bubble.Present(card, "Support Lapinosaur\nScan with your phone", null, support.QrCode);
                    else
                        bubble.Present(card, mode == "team-card-tooltip" ? "[types:3,5]Ally 4\n\nClick to select\nHover counters for matchups" : card.CounterTooltip(true));
                    if (mode == "team-support-tooltip-side")
                    {
                        var target = card.RectangleToScreen(card.ClientRectangle);
                        bubble.Relocate(new Rectangle(target.Right + 20, target.Bottom, bubble.Width, bubble.Height));
                    }
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(bubble.Width, bubble.Height))
                    {
                        bubble.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bubble.Size));
                        bitmap.Save(path);
                    }
                }
                return;
            }
            if (mode == "team-audit")
            {
                foreach (int appearance in new[] { 0, 2 })
                    foreach (float scale in new[] { .5f, .75f, 1f, 1.25f, 1.5f })
                        foreach (float font in new[] { .9f, 1f, 1.1f })
                        {
                            Theme.Appearance = appearance;
                            Theme.ApplyAppearance();
                            DisplaySettings.WindowScale = scale;
                            DisplaySettings.FontScale = font;
                            Save("team-keyboard", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "team-audit-" + appearance + "-" + (int)(scale * 100) + "-" + (int)(font * 100) + ".png"));
                        }
                return;
            }
            var plan = TeamTests.Sample();
            plan.Allies[0].Name = "";
            plan.Allies[2].Name = "";
            if (mode == "team-card-suggestions")
            {
                for (int i = 1; i < plan.Allies.Length; i++)
                    plan.Allies[i] = new TeamMember();
                foreach (var opponent in plan.Opponents)
                    opponent.Main = opponent.Sub = -1;
            }
            if (mode == "team-suggestions" || mode == "team-threats-grouped" || mode == "team-threats-matchup-tip")
                for (int i = 1; i < plan.Allies.Length; i++)
                    plan.Allies[i] = new TeamMember();
            using (PortableWindow form = mode == "team-editor" || mode == "team-suggestions" || mode.StartsWith("team-threats") || mode == "team-opponents" || mode == "team-keyboard" || mode == "team-matchups" ? (PortableWindow)new TeamEditor(plan, () => { }) : new MainForm())
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                if (form is MainForm)
                {
                    var stored = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(form);
                    for (int i = 0; i < 6; i++)
                    {
                        stored.Allies[i] = plan.Allies[i];
                        stored.Opponents[i] = plan.Opponents[i];
                    }
                    stored.Visible = true;
                    stored.Position = mode == "team-side" ? "Right" : "Bottom";
                    typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(form, null);
                    var board = (TeamBoard)typeof(MainForm).GetField("teamBoard", flags).GetValue(form);
                    board.SelectCard(mode == "team-card-suggestions" ? 2 : 0);
                    if (mode == "team-comparison" || mode == "team-comparison-six" || mode == "team-comparison-hover")
                    {
                        typeof(MainForm).GetMethod("ToggleInlineRadar", flags).Invoke(form, null);
                        foreach (var card in board.Controls.OfType<TeamCard>().Take(mode == "team-comparison-six" ? 6 : 3))
                            card.CompareCheck.Checked = true;
                    }
                }
                if (mode == "team-matchups")
                    typeof(TeamEditor).GetMethod("ShowMatchups", flags).Invoke(form, null);
                form.Show();
                Application.DoEvents();
                if (mode == "team-comparison-hover")
                    typeof(MainForm).GetField("teamPreview", flags).SetValue(form, 1);
                if (mode.StartsWith("team-threats"))
                {
                    ((SettingsButton)typeof(TeamEditor).GetField("threatToggle", flags).GetValue(form)).PerformClick();
                    if (mode == "team-threats-grouped" || mode == "team-threats-members")
                    {
                        var report = (ThreatReportPanel)typeof(TeamEditor).GetField("threatReport", flags).GetValue(form);
                        ((SettingsButton[])typeof(ThreatReportPanel).GetField("tabs", flags).GetValue(report))[mode == "team-threats-members" ? 2 : 1].PerformClick();
                    }
                    Application.DoEvents();
                }
                if (mode == "team-threats-matchup-tip" || mode == "team-threats-members-tip")
                {
                    var report = (ThreatReportPanel)typeof(TeamEditor).GetField("threatReport", flags).GetValue(form);
                    if (mode == "team-threats-members-tip")
                        ((SettingsButton[])typeof(ThreatReportPanel).GetField("tabs", flags).GetValue(report))[2].PerformClick();
                    var table = (DataGridView)typeof(ThreatReportPanel).GetField("table", flags).GetValue(report);
                    var longPlan = new TeamPlan();
                    longPlan.Allies[0].Main = 0;
                    longPlan.Allies[0].Sub = 6;
                    typeof(ThreatReportPanel).GetField("mode", flags).SetValue(report, 2);
                    report.UpdateReport(longPlan, false);
                    typeof(ThreatReportPanel).GetMethod("ShowCellTip", flags).Invoke(report, new object[] { 4, 0 });
                    var reportTips = (ApplicationTips)typeof(ThreatReportPanel).GetField("tips", flags).GetValue(report);
                    var painters = (System.Collections.Generic.Dictionary<Control, Action<Graphics, RectangleF>>)typeof(ApplicationTips).GetField("graphics", flags).GetValue(reportTips);
                    var sizes = (System.Collections.Generic.Dictionary<Control, Size>)typeof(ApplicationTips).GetField("graphicSizes", flags).GetValue(reportTips);
                    using (var bubble = new TipBubble())
                    {
                        bubble.ScrollGraphic = true;
                        bubble.SetDismissAction(() => bubble.Hide());
                        bubble.Present(table, table.Rows[0].Cells[4].ToolTipText, null, null, painters[table], sizes[table]);
                        Application.DoEvents();
                        using (var bitmap = new Bitmap(bubble.Width, bubble.Height))
                        {
                            bubble.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bubble.Size));
                            bitmap.Save(path);
                        }
                    }
                    return;
                }
                if (mode == "team-threats-help")
                {
                    var report = (ThreatReportPanel)typeof(TeamEditor).GetField("threatReport", flags).GetValue(form);
                    foreach (Control container in report.Controls)
                        foreach (Control control in container.Controls)
                            if (control.AccessibleName == "Show matchup legend")
                                using (var bubble = new TipBubble())
                                {
                                    bubble.Present(control, control.AccessibleDescription, null, null, report.DrawLegend, new Size(280, 250));
                                    Application.DoEvents();
                                    using (var bitmap = new Bitmap(bubble.Width, bubble.Height))
                                    {
                                        bubble.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bubble.Size));
                                        bitmap.Save(path);
                                    }
                                    return;
                                }
                }
                if (mode == "team-transition")
                {
                    var main = (MainForm)form;
                    main.FinishTeamTransition();
                    typeof(MainForm).GetMethod("ToggleInlineMatrix", flags).Invoke(main, null);
                    main.FinishTeamTransition();
                    var stored = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(main);
                    stored.Position = "Left";
                    typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(main, null);
                    main.FinishTeamTransition();
                    stored.Position = "Bottom";
                    typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(main, null);
                    var transition = (PanelTransition)typeof(MainForm).GetField("teamTransition", flags).GetValue(main);
                    foreach (double progress in new[] { 0, .2, .65, 1 })
                    {
                        transition.Advance(progress);
                        using (var frame = new Bitmap(main.Width, main.Height))
                        {
                            main.DrawToBitmap(frame, new Rectangle(Point.Empty, main.Size));
                            frame.Save(System.IO.Path.ChangeExtension(path, null) + "-" + (int)(progress * 100) + ".png");
                        }
                    }
                }
                if (mode == "team-opponents")
                {
                    foreach (var member in plan.Opponents)
                        member.Name = "";
                    foreach (Control control in form.Controls)
                        if (control is SettingsButton && control.Text == "Known opponents")
                            ((SettingsButton)control).PerformClick();
                    Application.DoEvents();
                }
                if (mode == "team-keyboard")
                {
                    var rows = (Control)typeof(TeamEditor).GetField("rows", flags).GetValue(form);
                    foreach (Control control in rows.Controls)
                        if (control is NameInput && control.TabIndex == 2)
                        {
                            ((NameInput)control).Editor.Focus();
                            break;
                        }
                    Application.DoEvents();
                }
                using (var image = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                    image.Save(path);
                }
            }
        }
    }
}
