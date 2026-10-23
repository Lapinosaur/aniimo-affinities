using System;
using System.Globalization;
using System.Linq;

namespace AniimoAffinities
{
    internal static class CalculationParityTests
    {
        internal static void Run()
        {
            if (TeamMember.JoinNames(new[] { "Ally 1", "Ally 2", "Ally 4" }, new[] { new TeamMember(), new TeamMember(), new TeamMember() }) != "Ally 1 & 2 & 4"
                || TeamMember.JoinNames(new[] { "Ally 1", "Astro", "Ally 3" }, new[] { new TeamMember(), new TeamMember { Name = "Astro" }, new TeamMember() }) != "Ally 1 & Astro & Ally 3"
                || TeamMember.JoinNames(new[] { "Ally 1", "Ally 2" }, new[] { new TeamMember(), new TeamMember { Name = "Ally 2" } }) != "Ally 1 & Ally 2")
                throw new Exception("Grouped automatic names must be compact without changing custom names.");
            for (int main = 0; main < Model.Names.Length; main++)
                for (int sub = -1; sub < Model.Names.Length; sub++)
                {
                    foreach (double value in Model.Factors(main, sub))
                        VerifyLabel(value);
                    foreach (int column in new[] { 0, 1 })
                        foreach (var targets in new[] { new int[0], Enumerable.Range(0, 9).ToArray(), Enumerable.Range(0, 9).Reverse().ToArray(), new[] { 4, 2, 4, 0, 8 } })
                        {
                            var combined = Model.Factors(main, sub);
                            var first = Model.Factors(main, -1);
                            var second = sub < 0 ? null : Model.Factors(sub, -1);
                            var expected = targets.Select(type =>
                            {
                                double sign = combined[column * 9 + type] - 1;
                                int mask = (first[column * 9 + type] - 1) * sign > .000001 ? 1 : 0;
                                if (second != null && (second[column * 9 + type] - 1) * sign > .000001)
                                    mask |= 2;
                                return new OriginEntry { Type = type, Mask = mask };
                            }).OrderBy(entry => entry.Mask == 2 ? 1 : entry.Mask == 3 ? .5 : 0).ToArray();
                            var actual = OriginGroups.Build(main, sub, column, targets);
                            if (!actual.Select(entry => entry.Type + ":" + entry.Mask).SequenceEqual(expected.Select(entry => entry.Type + ":" + entry.Mask)))
                                throw new Exception("Origin grouping changed its values or stable ordering.");
                        }
                }
            foreach (double value in new[] { 0, -0.0, -1.23456, .123456, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                VerifyLabel(value);
            var random = new Random(173);
            for (int sample = 0; sample < 30; sample++)
            {
                var plan = new TeamPlan();
                foreach (var member in plan.Allies.Concat(plan.Opponents))
                {
                    member.Main = random.Next(-1, 9);
                    member.Sub = member.Known ? random.Next(-1, 9) : -1;
                }
                if (sample % 3 == 0)
                    foreach (var member in plan.Opponents)
                        member.Main = member.Sub = -1;
                foreach (bool enemy in new[] { false, true })
                {
                    var team = enemy ? plan.Opponents : plan.Allies;
                    var targets = (enemy ? plan.Allies : plan.Opponents).Where(member => member.Known).ToArray();
                    if (targets.Length == 0)
                        targets = Enumerable.Range(0, 9).Select(type => new TeamMember { Main = type }).ToArray();
                    var before = team.Select(member => member.Main + ":" + member.Sub).ToArray();
                    var additions = TeamSuggestions.ComplementaryAdditions(plan, enemy);
                    var projected = (TeamMember[])team.Clone();
                    for (int slot = 0; slot < team.Length; slot++)
                    {
                        var addition = additions[slot];
                        if (addition != null && addition.Counter != (enemy ? plan.Allies : plan.Opponents).Any(target => target.Known))
                            throw new Exception("Suggestion context must follow known opposing members on either side.");
                        if (team[slot].Known)
                        {
                            if (addition != null)
                                throw new Exception("Complementary suggestions must keep selected members.");
                            continue;
                        }
                        int baseline = Coverage(projected, targets);
                        if (addition != null && (addition.AppliedAllies != team.Count(ally => ally.Known)
                            || addition.EarlierProposals.Length != projected.Count(ally => ally.Known) - team.Count(ally => ally.Known)))
                            throw new Exception("Projected suggestion context must distinguish applied members from earlier proposals.");
                        int maximum = TeamThreats.Candidates().Max(candidate => Coverage(projected.Select((member, index) => index == slot ? candidate : member).ToArray(), targets) - baseline);
                        if (addition == null)
                        {
                            if (maximum > 0)
                                throw new Exception("A useful complementary addition was omitted.");
                            continue;
                        }
                        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                        {
                            var target = targets[targetIndex];
                            var answer = addition.Answers[targetIndex];
                            if (answer.Main != target.Main || answer.Sub != target.Sub
                                || answer.Attackers != projected.Count(ally => ally.Known && TeamPlan.Damage(ally, target) > 1.0001)
                                || answer.Defenders != projected.Count(ally => ally.Known && TeamPlan.Damage(target, ally) < .9999)
                                || answer.Dealt != TeamPlan.Damage(addition.Types, target)
                                || answer.Received != TeamPlan.Damage(target, addition.Types))
                                throw new Exception("Suggestion evidence must match the projected team and actual matchup factors.");
                        }
                        projected[slot] = addition.Types;
                        if (addition.Gain < 0 || addition.Gain != maximum || Coverage(projected, targets) - baseline != addition.Gain)
                            throw new Exception("Each addition must improve coverage after preceding suggestions.");
                    }
                    if (!before.SequenceEqual(team.Select(member => member.Main + ":" + member.Sub)))
                        throw new Exception("Planning complementary additions must not mutate the actual team.");
                    foreach (var member in team)
                    {
                        var recommendation = TeamSuggestions.BestAddition(plan, member, enemy);
                        if (member.Known && recommendation != null)
                            throw new Exception("A recommendation must not replace a selected member.");
                        if (!member.Known)
                        {
                            int bestGain = 0;
                            foreach (var candidate in TeamThreats.Candidates())
                            {
                                var nextTeam = team.Select(ally => ReferenceEquals(ally, member) ? candidate : ally).ToArray();
                                int gain = Coverage(nextTeam, targets) - Coverage(team, targets);
                                if (gain > bestGain)
                                {
                                    bestGain = gain;
                                }
                            }
                            if (recommendation == null || recommendation.Gain != bestGain)
                                throw new Exception("Addition must prioritize new coverage and continue with backups after saturation.");
                            if (member.Known)
                                throw new Exception("Computing a recommendation must not select it.");
                        }
                        var actual = TeamSuggestions.Gains(plan, member, enemy);
                        for (int type = 0; type < 9; type++)
                        {
                            var selection = new Selection { Main = member.Main, Sub = member.Sub };
                            selection.Toggle(type);
                            var candidate = new TeamMember { Main = selection.Main, Sub = selection.Sub };
                            var next = team.Select(ally => ReferenceEquals(ally, member) ? candidate : ally).ToArray();
                            int expected = type == member.Main || type == member.Sub ? 0 : Coverage(next, targets) - Coverage(team, targets);
                            if (actual[type] != expected)
                                throw new Exception("Suggestion coverage changed from the reference calculation.");
                        }
                    }
                }
            }
        }
        private static int Coverage(TeamMember[] allies, TeamMember[] targets)
        {
            return targets.Sum(target => (allies.Any(ally => TeamPlan.Damage(ally, target) > 1.0001) ? 2 : 0) + (allies.Any(ally => TeamPlan.Damage(target, ally) < .9999) ? 1 : 0));
        }
        private static void VerifyLabel(double value)
        {
            if (Model.Factor(value) != "×" + value.ToString("0.###", CultureInfo.InvariantCulture))
                throw new Exception("Multiplier label formatting changed.");
        }
    }
}
