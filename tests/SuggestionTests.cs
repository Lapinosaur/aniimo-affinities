using System;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class SuggestionTests
    {
        internal static void Run()
        {
            VerifyComplementaryEditor();
            VerifyCardSuggestions();
            VerifySaturatedCoverage();
            VerifyRadarPreview();
            VerifySuggestionWindow();
            var hoverCard = new System.Drawing.Rectangle(80, 510, 150, 90);
            var hoverPopup = new System.Drawing.Rectangle(10, 10, 370, 490);
            if (!ApplicationTips.InHoverArea(hoverCard, hoverPopup, new System.Drawing.Point(175, 505))
                || ApplicationTips.InHoverArea(hoverCard, hoverPopup, new System.Drawing.Point(400, 505)))
                throw new Exception("The card-to-bubble gap must stay open without retaining unrelated outside areas.");
            var plan = new TeamPlan();
            var member = plan.Allies[0];
            member.Name = "Keep my name";
            int changes = 0;
            using (var host = new Form())
            using (var selector = new TeamTypeSelector(member, () => changes++))
            {
                host.Controls.Add(selector);
                host.Show();
                selector.SetSuggestions(plan);
                var best = TeamSuggestions.BestAddition(plan, member);
                var apply = selector.Controls.OfType<SettingsButton>().Single();
                string visualTip = TeamTypeSelector.RecommendationTip(best);
                if (!visualTip.StartsWith("[types:" + best.Types.Main + "," + best.Types.Sub + "]") || Model.Names.Any(name => visualTip.Contains(name)))
                    throw new Exception("Suggestion bubbles must show type icons instead of written type names.");
                if (!apply.AccessibleDescription.Contains(Model.Names[best.Types.Main]))
                    throw new Exception("Suggested types must remain named for screen readers.");
                if (best == null || !apply.Visible || member.Known || changes != 0)
                    throw new Exception("Empty members must show an unapplied recommendation.");
                selector.SetSuggestions(null);
                if (apply.Visible)
                    throw new Exception("Disabling suggestions must hide the apply action.");
                selector.SetSuggestions(plan);
                apply.PerformClick();
                if (member.Main != best.Types.Main || member.Sub != best.Types.Sub || member.Name != "Keep my name" || changes != 1 || apply.Visible)
                    throw new Exception("Apply must select the complete combination once and keep the name.");
                member.Main = member.Sub = -1;
                selector.SetSuggestions(plan);
                if (!selector.HandleShortcut(Keys.Control | Keys.Enter, false, true) || changes != 2 || !member.Known)
                    throw new Exception("Ctrl+Enter must apply the suggested combination.");
            }
        }
        private static void VerifySuggestionWindow()
        {
            var plan = new TeamPlan();
            plan.Allies[0].Main = 7;
            int calls = 0;
            using (var window = new SuggestionsDialog(plan, 4, (slot, member) => { calls++; return false; }))
            {
                window.Show();
                var cards = window.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<TeamCard>()).ToArray();
                if (window.ActiveSlot != 4 || cards.Count(card => card.Visible) != 5
                    || cards.Any(card => card.CompareCheck.Visible || card.PreviewButton.Visible || card.SuggestButton.Visible))
                    throw new Exception("Suggestion tabs must open on the requested slot and retain content without card action controls.");
                window.SelectSlot(2);
                if (window.ActiveSlot != 2 || plan.Allies[2].Known || calls != 0)
                    throw new Exception("Changing tabs must not apply proposals.");
                using (var tips = ApplicationTips.Create())
                {
                    var card = cards.Single(item => item.TabIndex == 2);
                    tips.SetToolTip(card, "Suggestion details");
                    int opened = 0;
                    tips.SetHoverBubble(card, true);
                    tips.SetOpenAction(card, () => opened++);
                    tips.ShowTip(card);
                    var enter = typeof(ApplicationTips).GetMethod("Enter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    using (var child = new SettingsButton())
                    {
                        card.Controls.Add(child);
                        enter.Invoke(tips, new object[] { child, EventArgs.Empty });
                    }
                    var bubble = (TipBubble)typeof(ApplicationTips).GetField("bubble", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(tips);
                    if (!bubble.Visible || bubble.Remaining != 0 || !bubble.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Close bubble").Visible)
                        throw new Exception("Hover bubbles must expose an explicit close action.");
                    bubble.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Open all suggestions").PerformClick();
                    if (opened != 1 || bubble.Visible)
                        throw new Exception("Opening suggestion details must invoke its action once and dismiss the transient bubble.");
                    tips.ShowTip(card);
                    bubble.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Close bubble").PerformClick();
                    if (bubble.Visible || opened != 1)
                        throw new Exception("Closing a bubble must not open its details.");
                    int applied = 0;
                    tips.SetApplyAction(card, () => applied++);
                    tips.ShowTip(card);
                    bubble.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Apply suggestion").PerformClick();
                    if (applied != 1 || opened != 1 || bubble.Visible)
                        throw new Exception("Applying from a bubble must run once, close it and never open the suggestions window.");
                    int previewed = 0;
                    tips.SetPreviewAction(card, () => previewed++);
                    tips.ShowTip(card);
                    bubble.Controls.OfType<SettingsButton>().Single(button => button.AccessibleName == "Preview suggestion on radars").PerformClick();
                    if (previewed != 1 || applied != 1 || opened != 1 || bubble.Visible)
                        throw new Exception("Radar preview must close the bubble and open only its preview without applying anything.");
                    tips.SetPreviewAction(card, null);
                    tips.SetApplyAction(card, null);
                    tips.SetHoverBubble(card, false);
                    tips.SetOpenAction(card, null);
                    tips.ShowTip(card);
                    if (bubble.Remaining != 1)
                        throw new Exception("Default tooltips must start with a full visible countdown.");
                    System.Threading.Thread.Sleep(25);
                    typeof(ApplicationTips).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(tips, new object[] { null, EventArgs.Empty });
                    if (bubble.Remaining <= 0 || bubble.Remaining >= 1)
                        throw new Exception("Tooltip countdown must decrease with elapsed time.");
                    if (bubble.Controls.OfType<SettingsButton>().Any(button => button.Visible))
                        throw new Exception("Default tooltips must not expose bubble controls.");
                }
            }
        }
        private static void VerifyRadarPreview()
        {
            var plan = TeamTests.Sample();
            plan.Allies[5].Main = plan.Allies[5].Sub = -1;
            var before = plan.Allies.Select(member => member.Main + ":" + member.Sub).ToArray();
            var suggestion = TeamSuggestions.ComplementaryAdditions(plan)[5];
            using (var selected = new SuggestionRadarDialog(plan, 5, suggestion, new[] { 0, 2 }))
            using (var all = new SuggestionRadarDialog(plan, 5, suggestion, new int[0]))
            {
                if (!selected.Series.Select(item => item.Slot).SequenceEqual(new[] { 0, 2, 5 }) || all.Series.Length != 6)
                    throw new Exception("Suggestion preview must compare checked allies, or all applied allies when none are checked.");
                if (selected.LegendSlotAt(new System.Drawing.Point(20, 75), 900) != 0
                    || selected.LegendSlotAt(new System.Drawing.Point(310, 75), 900) != 2
                    || selected.LegendSlotAt(new System.Drawing.Point(600, 75), 900) != 5
                    || selected.LegendSlotAt(new System.Drawing.Point(20, 130), 900) != -1)
                    throw new Exception("Legend hover must identify the actual series slot, including the suggestion.");
                selected.HighlightedSlot = 5;
                selected.Show();
                using (var bitmap = new System.Drawing.Bitmap(selected.Width, selected.Height))
                    selected.DrawToBitmap(bitmap, new System.Drawing.Rectangle(System.Drawing.Point.Empty, bitmap.Size));
                if (ReferenceEquals(selected.Series[0].Member, plan.Allies[0]) || ReferenceEquals(selected.Series.Last().Member, suggestion.Types))
                    throw new Exception("Preview series must be isolated snapshots.");
            }
            if (!before.SequenceEqual(plan.Allies.Select(member => member.Main + ":" + member.Sub)))
                throw new Exception("Opening and closing a preview must not apply any suggestion.");
            plan.Allies[5].Name = "Keep preview name";
            using (var board = new TeamBoard(plan, slot => { }))
            using (var preview = new SuggestionRadarDialog(plan, 5, suggestion, new[] { 0 }))
            {
                int changes = 0;
                board.TeamChanged += () => changes++;
                preview.Show();
                preview.ApplyButton.PerformClick();
                if (preview.DialogResult != DialogResult.OK || plan.Allies[5].Known)
                    throw new Exception("Apply must confirm the shown preview for its owner without changing data prematurely.");
                if (!board.ApplySuggestion(5, preview.Series.Last().Member) || changes != 1
                    || plan.Allies[5].Main != suggestion.Types.Main || plan.Allies[5].Sub != suggestion.Types.Sub
                    || plan.Allies[5].Name != "Keep preview name" || board.ComparedSlots.Length != 0)
                    throw new Exception("Applying a radar preview must preserve the name, apply its exact types and notify once.");
                if (board.ApplySuggestion(5, suggestion.Types) || changes != 1)
                    throw new Exception("Repeated preview application must not overwrite a filled slot.");
            }
        }
        private static void VerifySaturatedCoverage()
        {
            var plan = new TeamPlan();
            plan.Allies[0].Main = 7;
            plan.Allies[0].Sub = 4;
            plan.Allies[1].Main = 2;
            plan.Allies[1].Sub = 8;
            plan.Allies[2].Main = 0;
            plan.Allies[2].Sub = 1;
            var additions = TeamSuggestions.ComplementaryAdditions(plan);
            for (int slot = 3; slot < 6; slot++)
            {
                if (additions[slot] == null || additions[slot].Gain != 0 || additions[slot].Reinforcement <= 0 || plan.Allies[slot].Known)
                    throw new Exception("Fully covered teams must still suggest unapplied backup allies in every empty slot.");
                if (!TeamTypeSelector.RecommendationTip(additions[slot]).Contains("backup"))
                    throw new Exception("Backup suggestions must explain their purpose without claiming new coverage.");
            }
            if (additions.Skip(3).Select(item => item.Types.Main + ":" + item.Types.Sub).Distinct().Count() < 2)
                throw new Exception("Backup planning must diversify complementary answers.");
        }
        private static void VerifyCardSuggestions()
        {
            var plan = new TeamPlan();
            plan.Allies[0].Name = "Preserve this name";
            int selections = 0, saves = 0;
            using (var host = new Form())
            using (var board = new TeamBoard(plan, slot => selections++) { Dock = DockStyle.Fill })
            {
                host.Controls.Add(board);
                host.Show();
                board.TeamChanged += () => saves++;
                var cards = board.Controls.OfType<TeamCard>().OrderBy(card => card.TabIndex).ToArray();
                var expected = TeamSuggestions.ComplementaryAdditions(plan)[0];
                if (cards[0].CompareCheck.Visible || !cards[0].SuggestButton.Visible || !cards[0].PreviewButton.Visible || plan.Allies[0].Known)
                    throw new Exception("Empty ally cards must offer suggestions instead of comparison checkboxes.");
                cards[0].SuggestButton.PerformClick();
                if (!cards[0].CompareCheck.Visible || cards[0].SuggestButton.Visible || cards[0].PreviewButton.Visible || cards[0].CompareCheck.Checked || selections != 0 || saves != 1)
                    throw new Exception("Applying a suggestion must switch to an unchecked comparison checkbox without selecting the card.");
                if (plan.Allies[0].Main != expected.Types.Main || plan.Allies[0].Sub != expected.Types.Sub || plan.Allies[0].Name != "Preserve this name")
                    throw new Exception("Card suggestions must apply both elements and preserve the name.");
                var remaining = TeamSuggestions.ComplementaryAdditions(plan);
                for (int slot = 1; slot < cards.Length; slot++)
                    if (remaining[slot] == null ? cards[slot].Suggestion != null : cards[slot].Suggestion == null || cards[slot].Suggestion.Types.Main != remaining[slot].Types.Main || cards[slot].Suggestion.Types.Sub != remaining[slot].Types.Sub)
                        throw new Exception("Card suggestions must refresh together after application.");
                board.SelectCard(0);
                if (cards[0].CompareCheck.SurfaceColor() != Theme.AccentSurface)
                    throw new Exception("The checkbox background must match its selected card.");
                board.SelectCard(-1);
                if (cards[0].CompareCheck.SurfaceColor() != Theme.Surface)
                    throw new Exception("The checkbox background must follow ordinary cards too.");
            }
        }
        private static void VerifyComplementaryEditor()
        {
            var plan = new TeamPlan();
            using (var editor = new TeamEditor(plan, () => { }))
            {
                editor.Show();
                var filters = editor.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<TeamTypeSelector>()).OrderBy(filter => filter.TabIndex).ToArray();
                var field = typeof(TeamTypeSelector).GetField("recommendation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var expected = TeamSuggestions.ComplementaryAdditions(plan);
                if (filters.Length != 6 || expected.Count(item => item != null) < 2)
                    throw new Exception("An empty team must offer multiple complementary additions.");
                for (int round = 0; round < 2; round++)
                {
                    expected = TeamSuggestions.ComplementaryAdditions(plan);
                    for (int slot = 0; slot < filters.Length; slot++)
                    {
                        var actual = (TeamSuggestions.Recommendation)field.GetValue(filters[slot]);
                        if (expected[slot] == null ? actual != null : actual == null || actual.Types.Main != expected[slot].Types.Main || actual.Types.Sub != expected[slot].Types.Sub)
                            throw new Exception("Editor rows must share the complementary team plan.");
                    }
                    // Applying a later row first must recompute all earlier rows too.
                    if (round == 0)
                    {
                        int slot = Array.FindLastIndex(expected, item => item != null);
                        filters[slot].Controls.OfType<SettingsButton>().Single().PerformClick();
                        if (!plan.Allies[slot].Known)
                            throw new Exception("A later complementary suggestion could not be applied.");
                    }
                }
            }
        }
    }
}
