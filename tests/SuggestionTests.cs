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
                if (cards[0].CompareCheck.Visible || !cards[0].SuggestButton.Visible || plan.Allies[0].Known)
                    throw new Exception("Empty ally cards must offer suggestions instead of comparison checkboxes.");
                cards[0].SuggestButton.PerformClick();
                if (!cards[0].CompareCheck.Visible || cards[0].SuggestButton.Visible || cards[0].CompareCheck.Checked || selections != 0 || saves != 1)
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
