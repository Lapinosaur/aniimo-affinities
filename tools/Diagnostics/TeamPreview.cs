using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class TeamPreview
    {
        internal static void Save(string mode, string path)
        {
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
            using (PortableWindow form = mode == "team-editor" || mode == "team-opponents" || mode == "team-keyboard" || mode == "team-matchups" ? (PortableWindow)new TeamEditor(plan, () => { }) : new MainForm())
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
                    board.SelectCard(0);
                }
                if (mode == "team-matchups")
                    typeof(TeamEditor).GetMethod("ShowMatchups", flags).Invoke(form, null);
                form.Show();
                Application.DoEvents();
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
