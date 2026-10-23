using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class TeamTests
    {
        internal static TeamPlan Sample()
        {
            var plan = new TeamPlan { Visible = true };
            for (int i = 0; i < 6; i++)
            {
                plan.Allies[i].Name = new[] { "Ember", "Brook", "Breeze", "Moss", "Nova", "Frost" }[i];
                plan.Allies[i].Main = i;
                plan.Allies[i].Sub = (i + 2) % 9;
            }
            plan.Opponents[0].Name = "Opponent A";
            plan.Opponents[0].Main = 2;
            plan.Opponents[0].Sub = 7;
            plan.Opponents[1].Name = "Opponent B";
            plan.Opponents[1].Main = 3;
            return plan;
        }
        private static void VerifyTransitions()
        {
            using (var main = new MainForm())
            {
                main.Show();
                Application.DoEvents();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var team = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(main);
                var transition = (PanelTransition)typeof(MainForm).GetField("teamTransition", flags).GetValue(main);
                var apply = typeof(MainForm).GetMethod("ApplyTeamLayout", flags);
                team.Visible = false;
                apply.Invoke(main, null);
                transition.Finish();
                var baseline = main.Bounds;
                var work = Screen.FromControl(main).WorkingArea;
                main.Location = new Point(work.Left + 8, work.Top + 8);
                var toolbar = (Control)typeof(MainForm).GetField("teamToolbar", flags).GetValue(main);
                var caption = (Control)typeof(PortableWindow).GetField("captionPanel", flags).GetValue(main);
                var minimize = caption.Controls.Cast<Control>().First(c => c.AccessibleName == "Minimize");
                int fixedRight = main.Right;
                var fixedMinimize = minimize.RectangleToScreen(minimize.ClientRectangle);
                var commandBounds = caption.Controls.Cast<Control>().ToDictionary(control => control, control => control.RectangleToScreen(control.ClientRectangle));
                var fixedToolbar = toolbar.RectangleToScreen(toolbar.ClientRectangle);
                team.Visible = true;
                team.Position = "Left";
                apply.Invoke(main, null);
                transition.Finish();
                foreach (string destination in new[] { "Top", "Right", "Bottom", "Left" })
                {
                    var origin = transition.Capture();
                    team.Position = destination;
                    int nativeResizes = 0;
                    EventHandler countResize = (sender, args) => nativeResizes++;
                    main.SizeChanged += countResize;
                    apply.Invoke(main, null);
                    main.SizeChanged -= countResize;
                    if (SystemInformation.IsMenuAnimationEnabled && nativeResizes != 0)
                        throw new Exception("Preparing a swap must not resize the native window to the target and back");
                    var firstFrame = transition.Capture();
                    if (SystemInformation.IsMenuAnimationEnabled && Math.Abs(origin.Scale - firstFrame.Scale) < .001f && !origin.Cards.SequenceEqual(firstFrame.Cards))
                        throw new Exception("Changing panel axis must preserve card geometry at animation start.");
                    foreach (double progress in new[] { 0, .1, .35, .7, .95 })
                    {
                        transition.Advance(progress);
                        if (commandBounds.Any(item => item.Key.RectangleToScreen(item.Key.ClientRectangle) != item.Value))
                            throw new Exception("Caption command block moved during swap " + destination + " @ " + progress);
                        if (main.Right != fixedRight || minimize.RectangleToScreen(minimize.ClientRectangle) != fixedMinimize || toolbar.RectangleToScreen(toolbar.ClientRectangle) != fixedToolbar)
                            throw new Exception("Anchor drift " + destination + " @ " + progress + " right " + main.Right + "/" + fixedRight + " min " + minimize.RectangleToScreen(minimize.ClientRectangle) + "/" + fixedMinimize + " toolbar " + toolbar.RectangleToScreen(toolbar.ClientRectangle) + "/" + fixedToolbar);
                    }
                    var surface = (TransitionSurface)typeof(PanelTransition).GetField("surface", flags).GetValue(transition);
                    if (SystemInformation.IsMenuAnimationEnabled && (!surface.Visible || surface.Before == null || surface.After == null || surface.Parent.Controls.GetChildIndex(surface) != 0))
                        throw new Exception("Moving layouts must be presented as complete snapshots above live cards");
                    var frame = transition.Capture();
                    foreach (var cardBounds in frame.Cards)
                        if (cardBounds.X < 0 || cardBounds.Y < 0 || cardBounds.Right > frame.Board.Width + 1 || cardBounds.Bottom > frame.Board.Height + 1)
                            throw new Exception("Animated cards must stay inside their board during an axis swap.");
                    transition.Finish();
                    if (surface.Visible || surface.Before != null || surface.After != null)
                        throw new Exception("Animation snapshots must be released on completion");
                }
                team.Visible = false;
                apply.Invoke(main, null);
                transition.Finish();
                baseline = main.Bounds;
                foreach (string position in new[] { "Left", "Right", "Top", "Bottom", "Left", "Right" })
                {
                    team.Position = position;
                    team.Visible = true;
                    var before = transition.Capture();
                    apply.Invoke(main, null);
                    if (SystemInformation.IsMenuAnimationEnabled && transition.Capture().Window != before.Window)
                        throw new Exception("Panel animation must start at the displayed window bounds.");
                    transition.Advance(.4);
                    var middle = transition.Capture();
                    for (int click = 0; click < 31; click++)
                    {
                        team.Visible = !team.Visible;
                        apply.Invoke(main, null);
                    }
                    if (SystemInformation.IsMenuAnimationEnabled && transition.Capture().Content != middle.Content)
                        throw new Exception("Queued panel reversal must retain displayed content geometry.");
                    transition.Advance(1);
                    transition.Advance(1);
                    if (main.Size != baseline.Size || transition.Capture().Visible)
                        throw new Exception("Repeated panel reversals must end hidden without size drift.");
                }
            }
        }
        internal static void Run()
        {
            foreach (var candidatePlan in new[] { new TeamPlan(), Sample() })
                foreach (bool enemy in new[] { false, true })
                {
                    var team = enemy ? candidatePlan.Opponents : candidatePlan.Allies;
                    foreach (var member in team)
                    {
                        int mainBefore = member.Main, subBefore = member.Sub;
                        var gains = TeamSuggestions.Gains(candidatePlan, member, enemy);
                        for (int type = 0; type < Model.Names.Length; type++)
                            if (gains[type] != TeamSuggestions.Gain(candidatePlan, member, type, enemy))
                                throw new Exception("Batch suggestions must preserve every individual score on both sides.");
                        if (member.Main != mainBefore || member.Sub != subBefore)
                            throw new Exception("Suggestion evaluation must not mutate team selections.");
                        var counts = candidatePlan.CountMatchups(member);
                        var known = candidatePlan.Opponents.Where(target => target.Known).ToArray();
                        if (counts.Total != known.Length || counts.Strong != known.Count(target => TeamPlan.Damage(member, target) > 1.0001) || counts.Weak != known.Count(target => TeamPlan.Damage(target, member) > 1.0001))
                            throw new Exception("Shared card counts must retain exact matchup totals, including unknown members.");
                    }
                }
            var advicePlan = new TeamPlan();
            var threats = TeamThreats.Candidates();
            if (threats.Length != 45 || threats.Count(threat => threat.Sub < 0) != 9 || threats.Any(threat => threat.Sub >= 0 && threat.Sub <= threat.Main))
                throw new Exception("Threat report must enumerate unique singles and unordered dual types");
            if (!TeamThreats.Describe(advicePlan).Contains("Choose at least one ally"))
                throw new Exception("Unknown team must not receive a threat ranking");
            advicePlan.Allies[0].Main = 0;
            advicePlan.Allies[0].Name = "First ally";
            string report = TeamThreats.Describe(advicePlan);
            if (!report.Contains("First ally") || !report.Contains("MOST EXPOSED ALLIES") || !report.Contains("DUAL-TYPE THREATS") || report.Contains("Ally 2"))
                throw new Exception("Threat report must identify known vulnerable allies and exclude unknown slots");
            advicePlan.Allies[0].Main = -1;
            if (Enumerable.Range(0, 9).Max(type => TeamSuggestions.Gain(advicePlan, advicePlan.Allies[0], type)) <= 0)
                throw new Exception("The first empty ally must receive suggestions against the nine single types");
            for (int type = 0; type < 9; type++)
            {
                var candidate = new TeamMember { Main = type };
                int expected = Enumerable.Range(0, 9).Sum(targetType =>
                {
                    var target = new TeamMember { Main = targetType };
                    return (TeamPlan.Damage(candidate, target) > 1.0001 ? 2 : 0) + (TeamPlan.Damage(target, candidate) < .9999 ? 1 : 0);
                });
                if (TeamSuggestions.Gain(advicePlan, advicePlan.Allies[0], type) != expected || TeamSuggestions.Gain(advicePlan, advicePlan.Allies[5], type) != expected)
                    throw new Exception("Initial suggestions must use actual coverage regardless of slot order");
            }
            if (advicePlan.Allies.Any(ally => ally.Known))
                throw new Exception("Initial suggestions must not select elements automatically");
            advicePlan.Allies[5].Main = 0;
            if (TeamSuggestions.Gain(advicePlan, advicePlan.Allies[0], 0) != 0)
                throw new Exception("First ally suggestions must account for coverage already provided by later allies");
            var laterCoverage = Enumerable.Range(0, 9).Select(type => TeamSuggestions.Gain(advicePlan, advicePlan.Allies[0], type)).ToArray();
            advicePlan.Allies[5].Main = -1;
            advicePlan.Allies[1].Main = 0;
            if (!laterCoverage.SequenceEqual(Enumerable.Range(0, 9).Select(type => TeamSuggestions.Gain(advicePlan, advicePlan.Allies[0], type))))
                throw new Exception("Suggestion ranking must not depend on ally order");
            advicePlan.Allies[1].Main = -1;
            advicePlan.Opponents[0].Main = 0;
            var adviceGains = Enumerable.Range(0, 9).Select(type => TeamSuggestions.Gain(advicePlan, advicePlan.Allies[0], type)).ToArray();
            if (adviceGains.Max() <= 0 || advicePlan.Allies[0].Known)
                throw new Exception("Suggestions should evaluate known opponents without mutating the team");
            for (int type = 0; type < 9; type++)
            {
                var candidate = new TeamMember { Main = type };
                int expected = (TeamPlan.Damage(candidate, advicePlan.Opponents[0]) > 1.0001 ? 2 : 0) + (TeamPlan.Damage(advicePlan.Opponents[0], candidate) < .9999 ? 1 : 0);
                if (adviceGains[type] != expected)
                    throw new Exception("Suggestion score must explain offensive coverage and defensive answers");
            }
            VerifyTransitions();
            var mirrored = Sample();
            using (var reportHost = new Form { ClientSize = new Size(618, 280) })
            using (var reportView = new ThreatReportPanel { Dock = DockStyle.Fill })
            {
                reportHost.Controls.Add(reportView);
                reportHost.Show();
                Application.DoEvents();
                reportView.UpdateReport(mirrored, false);
                var legendButton = reportView.Controls.Cast<Control>().SelectMany(control => control.Controls.Cast<Control>()).OfType<SettingsButton>().Single(button => button.AccessibleName == "Show matchup legend");
                reportHost.Activate();
                Application.DoEvents();
                legendButton.PerformClick();
                var reportTips = (ApplicationTips)typeof(ThreatReportPanel).GetField("tips", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reportView);
                var legendBubble = (TipBubble)typeof(ApplicationTips).GetField("bubble", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reportTips);
                if (!legendBubble.Visible)
                    throw new Exception("Clicking report information must immediately show the legend");
                var grid = reportView.Controls.OfType<DataGridView>().Single();
                if (grid.Rows.Count == 0 || grid.Columns.Count != 5)
                    throw new Exception("Visual threat report must expose a structured table");
                var slim = grid.Controls.OfType<SlimScrollBar>().Single();
                if (grid.ScrollBars != ScrollBars.None || !slim.Visible)
                    throw new Exception("Overflowing report must use the slim scrollbar instead of native chrome");
                typeof(SlimDataGridView).GetMethod("OnMouseWheel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(grid, new object[] { new MouseEventArgs(MouseButtons.None, 0, 10, 40, -120) });
                if (grid.FirstDisplayedScrollingRowIndex <= 0)
                    throw new Exception("Report mouse wheel must still scroll without native scrollbars");
                var reportTabs = (SettingsButton[])typeof(ThreatReportPanel).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reportView);
                reportTabs[2].PerformClick();
                if (grid.Rows.Count != 6 || grid.Columns[0].HeaderText != "Member")
                    throw new Exception("Members report must list each known member");
                foreach (DataGridViewRow row in grid.Rows)
                {
                    var member = mirrored.Allies.Single(item => item.Name == Convert.ToString(row.Cells[0].Value));
                    var damage = mirrored.Opponents.Where(item => item.Known).Select(item => TeamPlan.Damage(item, member)).ToArray();
                    if (Convert.ToString(row.Cells[1].Value) != damage.Count(value => value > 1.0001) + "/" + damage.Length ||
                        Convert.ToString(row.Cells[2].Value) != damage.Count(value => value < .9999) + "/" + damage.Length ||
                        Convert.ToString(row.Cells[3].Value) != Model.Factor(damage.Max()))
                        throw new Exception("Report aggregates must match the domain's exact incoming exchanges.");
                }
                reportView.UpdateReport(mirrored, true);
                if (grid.Rows.Count != 2)
                    throw new Exception("Visual report must refresh when switching teams and exclude unknown members");
                if (slim.Visible)
                    throw new Exception("Slim scrollbar must disappear when all rows fit");
                var groupedPlan = new TeamPlan();
                groupedPlan.Allies[0].Main = 0;
                reportView.UpdateReport(groupedPlan, false);
                reportTabs[1].PerformClick();
                var expectedPairs = TeamThreats.Candidates().Where(pair => pair.Sub >= 0 && TeamPlan.Damage(pair, groupedPlan.Allies[0]) > 1.0001).ToArray();
                var displayedPairs = grid.Rows.Cast<DataGridViewRow>().SelectMany(row => Convert.ToString(row.Cells[0].Value).Split(new[] { "; " }, StringSplitOptions.None)).ToArray();
                if (grid.Rows.Count >= expectedPairs.Length || displayedPairs.Length != expectedPairs.Length || displayedPairs.Distinct().Count() != expectedPairs.Length)
                    throw new Exception("Dual threats must be grouped without losing or duplicating combinations");
                foreach (var pair in expectedPairs)
                    if (!displayedPairs.Contains(Model.Names[pair.Main] + " + " + Model.Names[pair.Sub]))
                        throw new Exception("Grouped report must retain every original dual threat");
                foreach (DataGridViewRow row in grid.Rows)
                {
                    var pairNames = Convert.ToString(row.Cells[0].Value).Split(new[] { "; " }, StringSplitOptions.None);
                    var pairs = expectedPairs.Where(pair => pairNames.Contains(Model.Names[pair.Main] + " + " + Model.Names[pair.Sub])).ToArray();
                    if (pairs.Select(pair => TeamPlan.Damage(groupedPlan.Allies[0], pair)).Distinct().Count() != 1)
                        throw new Exception("Groups must split when the team's outgoing damage differs");
                    double reply = TeamPlan.Damage(groupedPlan.Allies[0], pairs[0]);
                    string category = reply < .9999 ? "disadvantaged" : reply > 1.0001 ? "trade blows" : "pressured";
                    if (!Convert.ToString(row.Cells[4].Value).Contains(category))
                        throw new Exception("Matchup summary must distinguish reduced, neutral and strong replies");
                    if (!row.Cells[1].ToolTipText.Contains("received") || !row.Cells[1].ToolTipText.Contains("dealt") || !row.Cells[2].ToolTipText.Contains("None"))
                        throw new Exception("Counter tooltips must explain exact exchanges and empty resistance sets");
                }
            }
            var reversed = new TeamPlan();
            for (int slot = 0; slot < 6; slot++)
            {
                reversed.Allies[slot] = mirrored.Opponents[slot];
                reversed.Opponents[slot] = mirrored.Allies[slot];
            }
            for (int slot = 0; slot < 6; slot++)
                for (int type = 0; type < 9; type++)
                    if (TeamSuggestions.Gain(mirrored, mirrored.Opponents[slot], type, true) != TeamSuggestions.Gain(reversed, reversed.Allies[slot], type))
                        throw new Exception("Both teams must receive identical suggestions when their roles are reversed");
            string enemyReport = TeamThreats.Describe(mirrored, true);
            if (!enemyReport.Contains("MOST EXPOSED OPPONENTS") || !enemyReport.Contains("against your known allies") || !enemyReport.Contains("Opponent A"))
                throw new Exception("Opponent report must describe the active side and assess it against your team");
            using (var main = new MainForm())
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var state = (Selection)typeof(MainForm).GetField("selection", flags).GetValue(main);
                var team = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(main);
                var board = (TeamBoard)typeof(MainForm).GetField("teamBoard", flags).GetValue(main);
                var card = board.Controls.OfType<TeamCard>().First();
                state.Main = 7;
                state.Sub = -1;
                team.Allies[0].Main = 2;
                team.Allies[0].Sub = 4;
                var enter = typeof(Control).GetMethod("OnMouseEnter", flags);
                var leave = typeof(Control).GetMethod("OnMouseLeave", flags);
                var previewProperty = typeof(MainForm).GetProperty("preview", flags);
                enter.Invoke(card, new object[] { EventArgs.Empty });
                var shown = (Selection)previewProperty.GetValue(main, null);
                if (shown.Main != 2 || shown.Sub != 4 || state.Main != 7 || state.Sub != -1)
                    throw new Exception("Card hover must preview both elements without committing selection");
                leave.Invoke(card, new object[] { EventArgs.Empty });
                if (((Selection)previewProperty.GetValue(main, null)).Main != 7)
                    throw new Exception("Leaving a card must restore the selected elements");
                enter.Invoke(card, new object[] { EventArgs.Empty });
                typeof(Control).GetMethod("OnClick", flags).Invoke(card, new object[] { EventArgs.Empty });
                leave.Invoke(card, new object[] { EventArgs.Empty });
                if (state.Main != 2 || state.Sub != 4)
                    throw new Exception("Click must retain the previewed ally after mouse leave");
                if (board.SelectedIndex != 0 || !card.Selected)
                    throw new Exception("Selected card must remain marked after mouse leave");
                enter.Invoke(board.Controls.OfType<TeamCard>().Skip(1).First(), new object[] { EventArgs.Empty });
                if (board.SelectedIndex != 0 || !card.Selected)
                    throw new Exception("Previewing another card must preserve the selected card");
                board.SyncSelection(7, -1);
                if (board.SelectedIndex != -1 || card.Selected)
                    throw new Exception("Changing elements must clear the obsolete card selection");
            }
            using (var host = new Form())
            using (var anchor = new Button { Size = new Size(140, 32) })
            using (var bubble = new TipBubble())
            {
                host.Controls.Add(anchor);
                host.Show();
                bubble.Present(anchor, "Support");
                var target = anchor.RectangleToScreen(anchor.ClientRectangle);
                bubble.Relocate(new Rectangle(target.Right + 20, target.Top, bubble.Width, bubble.Height));
                if (bubble.ArrowSide != 1)
                    throw new Exception("Right-side tooltip must point left");
                bubble.Relocate(new Rectangle(target.Left - bubble.Width - 20, target.Top, bubble.Width, bubble.Height));
                if (bubble.ArrowSide != 2)
                    throw new Exception("Left-side tooltip must point right");
                bubble.Relocate(new Rectangle(target.Left, target.Bottom + 10, bubble.Width, bubble.Height));
                if (bubble.ArrowSide != 0)
                    throw new Exception("Tooltip must restore its vertical arrow");
            }
            foreach (string position in new[] { "Left", "Right", "Top", "Bottom" })
                using (var host = new Form())
                using (var board = new TeamBoard(new TeamPlan { Position = position }, index => { }))
                {
                    board.Size = position == "Left" || position == "Right" ? new Size(216, 480) : new Size(720, 104);
                    host.Controls.Add(board);
                    host.CreateControl();
                    var original = board.Controls.Cast<Control>().Select(c => c.Bounds).ToArray();
                    var obstacle = new Rectangle(0, 0, 60, 40);
                    board.AvoidSupportBubble(board.RectangleToScreen(obstacle));
                    foreach (Control card in board.Controls)
                        if (card.Bounds.IntersectsWith(obstacle))
                            throw new Exception("Support bubble hides an ally card at " + position);
                    for (int i = 0; i < original.Length; i++)
                    {
                        var current = board.Controls[i].Bounds;
                        if (current.Y != original[i].Y || current.Height != original[i].Height || !board.ClientRectangle.Contains(current))
                            throw new Exception("Support bubble moved an ally vertically or outside the panel");
                        if (!original[i].IntersectsWith(obstacle) && current != original[i])
                            throw new Exception("Support bubble changed an unaffected ally");
                    }
                    board.AvoidSupportBubble(Rectangle.Empty);
                    if (!original.SequenceEqual(board.Controls.Cast<Control>().Select(c => c.Bounds)))
                        throw new Exception("Ally layout was not restored after support tooltip closed");
                }
            var plan = Sample();
            var first = plan.Allies[0];
            if (!double.IsNaN(TeamPlan.Damage(first, plan.Opponents[5])))
                throw new Exception("Unknown opponent must not produce a neutral matchup");
            for (int a = 0; a < 9; a++)
                for (int b = -1; b < 9; b++)
                    for (int target = 0; target < 9; target++)
                    {
                        var attacker = new TeamMember { Main = a, Sub = b == a ? -1 : b };
                        var defender = new TeamMember { Main = target, Sub = (target + 1) % 9 };
                        double expected = Model.Factors(a, -1)[target] * Model.Factors(a, -1)[defender.Sub];
                        if (attacker.Sub >= 0)
                            expected = Math.Max(expected, Model.Factors(b, -1)[target] * Model.Factors(b, -1)[defender.Sub]);
                        if (Math.Abs(TeamPlan.Damage(attacker, defender) - expected) > .001)
                            throw new Exception("Team matchup parity failed");
                    }
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            try
            {
                first.Name = "Étoile | 🐇";
                plan.Position = "Left";
                TeamPreferences.Save(path, plan);
                var loaded = TeamPreferences.Load(path);
                if (loaded.Allies[0].Name != first.Name || loaded.Position != "Left" || !loaded.Visible || loaded.Opponents[5].Known)
                    throw new Exception("Team preferences roundtrip failed");
                File.WriteAllText(path, "broken");
                if (TeamPreferences.Load(path).Allies.Any(x => x.Known))
                    throw new Exception("Corrupt team fallback failed");
            }
            finally { File.Delete(path); }
            using (var form = new MainForm())
            {
                form.Show();
                Application.DoEvents();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var state = (TeamPlan)typeof(MainForm).GetField("teamPlan", flags).GetValue(form);
                var canvas = (Control)typeof(MainForm).GetField("canvas", flags).GetValue(form);
                var baseline = canvas.Size;
                var baselineWindow = form.Size;
                var workArea = Screen.FromControl(form).WorkingArea;
                form.Location = new System.Drawing.Point(workArea.Right - form.Width - 24, workArea.Top + 24);
                int anchoredRight = form.Right, anchoredTop = form.Top;
                foreach (string position in new[] { "Top", "Bottom", "Left", "Right" })
                {
                    state.Visible = true;
                    state.Position = position;
                    typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(form, null);
                    form.FinishTeamTransition();
                    Application.DoEvents();
                    var positionToggle = (SettingsButton)typeof(MainForm).GetField("teamToggle", flags).GetValue(form);
                    if (positionToggle.Glyph != "team-" + position)
                        throw new Exception("Team icon must follow panel position");
                    bool side = position == "Left" || position == "Right";
                    int panelWidth = side ? (int)(216 * form.AppliedWindowScale) : 0;
                    int panelHeight = side ? 0 : (int)(104 * form.AppliedWindowScale);
                    int targetWidth = Math.Max(form.MinimumSize.Width, Math.Min(baselineWindow.Width + panelWidth, anchoredRight - workArea.Left));
                    int targetHeight = Math.Max(form.MinimumSize.Height, Math.Min(baselineWindow.Height + panelHeight, workArea.Bottom - anchoredTop));
                    var expected = new Size(baseline.Width + targetWidth - baselineWindow.Width - panelWidth, baseline.Height + targetHeight - baselineWindow.Height - panelHeight);
                    if (Math.Abs(canvas.Width - expected.Width) > 3 || Math.Abs(canvas.Height - expected.Height) > 3)
                        throw new Exception("Team content size must account for available screen space: " + position + " expected " + expected + " actual " + canvas.Size);
                    if (form.Right != anchoredRight || form.Top != anchoredTop)
                        throw new Exception("Team layout must preserve the top-right window corner: " + position);
                }
                state.Visible = false;
                typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(form, null);
                form.FinishTeamTransition();
                if (canvas.Size != baseline)
                    throw new Exception("Hiding team board must restore content size");
                var toggle = (SettingsButton)typeof(MainForm).GetField("teamToggle", flags).GetValue(form);
                var directions = (SettingsButton[])typeof(MainForm).GetField("teamPositions", flags).GetValue(form);
                var toggleBounds = toggle.Bounds;
                var toggleShape = toggle.GroupShape;
                if (directions.Any(b => b.Visible) || !toggle.Segment || toggleShape.Width != 67 || toggle.Glyph.Length == 0)
                    throw new Exception("Hidden team must show only its icon toggle");
                state.Allies[0].Main = 0;
                state.Allies[0].Sub = 2;
                state.Visible = true;
                typeof(MainForm).GetMethod("ApplyTeamLayout", flags).Invoke(form, null);
                form.FinishTeamTransition();
                if (toggle.Bounds != toggleBounds || !toggle.Segment || directions.Any(b => !b.Visible))
                    throw new Exception("Team directions must expand without moving the toggle");
                var board = (Control)typeof(MainForm).GetField("teamBoard", flags).GetValue(form);
                ((Button)board.Controls[0]).PerformClick();
                var selected = (Selection)typeof(MainForm).GetField("selection", flags).GetValue(form);
                if (selected.Main != 0 || selected.Sub != 2)
                    throw new Exception("Team card did not load member into current view");
                var card = board.Controls[0];
                var elementBounds = (System.Drawing.RectangleF)typeof(TeamCard).GetMethod("ElementBounds", flags).Invoke(card, new object[] { 1 });
                float zoom = WindowZoom.Factor(card);
                typeof(TeamCard).GetMethod("OnMouseMove", flags).Invoke(card, new object[] { new MouseEventArgs(MouseButtons.None, 0, (int)((elementBounds.X + 5) * zoom), (int)((elementBounds.Y + 5) * zoom), 0) });
                if ((int)typeof(MainForm).GetField("teamHovered", flags).GetValue(form) != 2 || selected.Main != 0 || selected.Sub != 2)
                    throw new Exception("Hovering a team element must highlight without changing selection");
                typeof(TeamCard).GetMethod("OnMouseLeave", flags).Invoke(card, new object[] { EventArgs.Empty });
                if ((int)typeof(MainForm).GetField("teamHovered", flags).GetValue(form) != -1)
                    throw new Exception("Leaving a team element must clear its highlight");
                state.Opponents[0].Main = 2;
                state.Opponents[0].Sub = 7;
                foreach (int counter in new[] { 0, 1 })
                {
                    int expectedMask = 0;
                    foreach (var opponent in state.Opponents.Where(x => x.Known && (counter == 0 ? TeamPlan.Damage(state.Allies[0], x) : TeamPlan.Damage(x, state.Allies[0])) > 1.0001))
                    {
                        expectedMask |= 1 << opponent.Main;
                        if (opponent.Sub >= 0)
                            expectedMask |= 1 << opponent.Sub;
                    }
                    typeof(TeamCard).GetMethod("SetCounter", flags).Invoke(card, new object[] { counter });
                    if ((int)typeof(MainForm).GetField("teamOpponentMask", flags).GetValue(form) != expectedMask || selected.Main != 0 || selected.Sub != 2)
                        throw new Exception("Counter hover must highlight affected opponent types without changing selection");
                }
                typeof(TeamCard).GetMethod("OnMouseLeave", flags).Invoke(card, new object[] { EventArgs.Empty });
                if ((int)typeof(MainForm).GetField("teamOpponentMask", flags).GetValue(form) != 0)
                    throw new Exception("Leaving counter must clear opponent highlights");
            }
            var resetPlan = Sample();
            using (var editor = new TeamEditor(resetPlan, () => { }))
            {
                editor.Show();
                Application.DoEvents();
                var clear = editor.Controls.OfType<SettingsButton>().Single(b => b.Text == "Clear all");
                clear.PerformClick();
                if (!resetPlan.Allies[0].Known)
                    throw new Exception("Clear all must require confirmation");
                clear.PerformClick();
                if (resetPlan.Allies.Any(m => m.Known || m.Name.Length != 0) || !resetPlan.Opponents[0].Known)
                    throw new Exception("Clear all must only reset the displayed team");
                editor.Controls.OfType<SettingsButton>().Single(b => b.Text == "Known opponents").PerformClick();
                clear.PerformClick();
                editor.Controls.OfType<SettingsButton>().Single(b => b.Text == "Your team").PerformClick();
                if (clear.Text != "Clear all")
                    throw new Exception("Changing tabs must cancel clear confirmation");
                editor.Close();
            }
            int saves = 0;
            using (var editor = new TeamEditor(plan, () => saves++))
            {
                editor.Show();
                Application.DoEvents();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var rows = (Control)typeof(TeamEditor).GetField("rows", flags).GetValue(editor);
                var selector = rows.Controls.OfType<TeamTypeSelector>().First().Controls.OfType<TypeSelector>().Single();
                var icons = selector.Controls.OfType<IconButton>().ToArray();
                icons.Single(b => b.Type < 0).PerformClick();
                icons.Single(b => b.Type == 2).PerformClick();
                icons.Single(b => b.Type == 3).PerformClick();
                if (plan.Allies[0].Main != 2 || plan.Allies[0].Sub != 3 || icons.Single(b => b.Type == 2).Badge != "Main" || icons.Single(b => b.Type == 3).Badge != "Sub")
                    throw new Exception("Team icon selection and badges failed");
                icons.Single(b => b.Type == 2).PerformClick();
                if (plan.Allies[0].Main != 3 || plan.Allies[0].Sub != -1 || icons.Single(b => b.Type == 3).Badge != "Main")
                    throw new Exception("Removing Main must promote Sub");
                rows.Controls.OfType<NameInput>().First().Editor.Text = "New name";
                var inputs = rows.Controls.OfType<NameInput>().OrderBy(n => n.TabIndex).ToArray();
                var filter = rows.Controls.OfType<TeamTypeSelector>().First();
                inputs[0].Editor.Focus();
                var editorKey = typeof(TeamEditor).GetMethod("ProcessCmdKey", flags);
                editorKey.Invoke(editor, new object[] { Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Control | Keys.D0 });
                if (plan.Allies[0].Known)
                    throw new Exception("Ctrl+0 from name must clear its row");
                editorKey.Invoke(editor, new object[] { Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Control | Keys.D1 });
                int chosen = plan.Allies[0].Main;
                editorKey.Invoke(editor, new object[] { Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, new IntPtr(1L << 30)), Keys.Control | Keys.D1 });
                if (chosen < 0 || plan.Allies[0].Main != chosen)
                    throw new Exception("Held shortcut must not repeatedly toggle");
                editor.SelectNextControl(inputs[0].Editor, true, true, true, true);
                if (!inputs[1].Editor.Focused)
                    throw new Exception("Tab from name must move directly to next name");
                if (inputs[0].NavigationHint != "Shift+Tab" || inputs[2].NavigationHint != "Tab" || icons.Any(b => b.Shortcut.Length > 0))
                    throw new Exception("Name navigation badges must follow active row");
                var secondFilter = rows.Controls.OfType<TeamTypeSelector>().OrderBy(n => n.TabIndex).Skip(1).First();
                if (secondFilter.Controls[0].Controls.OfType<IconButton>().Any(b => b.Shortcut != (b.Type < 0 ? 0 : b.TabIndex + 1).ToString()))
                    throw new Exception("Active name row must show element shortcuts");
                editor.SelectNextControl(inputs[1].Editor, false, true, true, true);
                if (!inputs[0].Editor.Focused)
                    throw new Exception("Shift+Tab must move directly to previous name");
                icons.First(b => b.Type >= 0).Focus();
                var keyMethod = typeof(TeamTypeSelector).GetMethod("ProcessCmdKey", flags);
                keyMethod.Invoke(filter, new object[] { Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Tab });
                if (!inputs[1].Editor.Focused)
                    throw new Exception("Tab must leave filter for next name");
                icons.First(b => b.Type >= 0).Focus();
                keyMethod.Invoke(filter, new object[] { Message.Create(IntPtr.Zero, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Shift | Keys.Tab });
                if (!inputs[0].Editor.Focused)
                    throw new Exception("Shift+Tab must return to the same row name");
                float originalFont = DisplaySettings.FontScale;
                float originalScale = DisplaySettings.WindowScale;
                bool originalBadges = DisplaySettings.ShowBadges;
                try
                {
                    inputs[2].Editor.Text = "";
                    foreach (float scale in new[] { .5f, .75f, 1f, 1.25f, 1.5f })
                        foreach (float font in new[] { .9f, 1f, 1.1f })
                        {
                            DisplaySettings.ApplyDisplay(font, true, scale);
                            inputs[1].Editor.Focus();
                            foreach (var input in inputs)
                                foreach (Control child in input.Controls)
                                    if (child.Top < 12 || child.Bottom > input.Height - 1 || child.Right > input.Width - 1)
                                        throw new Exception("Name content overlaps badge strip or field border");
                        }
                }
                finally { DisplaySettings.ApplyDisplay(originalFont, originalBadges, originalScale); }
                editor.Close();
                if (saves != 1 || plan.Allies[0].Name != "New name")
                    throw new Exception("Closing editor must flush pending autosave once");
            }
        }
    }
}
