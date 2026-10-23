using System;
using System.Linq;

namespace AniimoAffinities
{
    internal static class TeamSuggestions
    {
        internal sealed class Recommendation
        {
            internal TeamMember Types;
            internal int Gain;
            internal int Reinforcement;
            internal Answer[] Answers;
            internal bool Counter;
            internal int AppliedAllies;
            internal TeamMember[] EarlierProposals = new TeamMember[0];
        }
        internal sealed class Answer
        {
            internal int Main, Sub, Attackers, Defenders;
            internal double Dealt, Received;
        }
        internal static Recommendation BestAddition(TeamPlan plan, TeamMember member, bool enemy = false)
        {
            if (member.Known)
                return null;
            var team = enemy ? plan.Opponents : plan.Allies;
            var targets = Targets(plan, enemy);
            var result = BestAddition(team, targets, member, (enemy ? plan.Allies : plan.Opponents).Any(target => target.Known));
            if (result != null)
                result.AppliedAllies = team.Count(ally => ally.Known);
            return result;
        }
        internal static Recommendation[] ComplementaryAdditions(TeamPlan plan, bool enemy = false)
        {
            var actual = enemy ? plan.Opponents : plan.Allies;
            var projected = (TeamMember[])actual.Clone();
            var targets = Targets(plan, enemy);
            bool counter = (enemy ? plan.Allies : plan.Opponents).Any(target => target.Known);
            var result = new Recommendation[actual.Length];
            for (int slot = 0; slot < actual.Length; slot++)
            {
                if (actual[slot].Known)
                    continue;
                result[slot] = BestAddition(projected, targets, actual[slot], counter);
                if (result[slot] != null)
                {
                    result[slot].AppliedAllies = actual.Count(ally => ally.Known);
                    result[slot].EarlierProposals = projected.Where((ally, index) => !actual[index].Known && ally.Known).ToArray();
                    projected[slot] = result[slot].Types;
                }
            }
            return result;
        }
        private static Recommendation BestAddition(TeamMember[] team, TeamMember[] targets, TeamMember member, bool counter)
        {
            int baseline = Coverage(team, targets);
            Recommendation best = null;
            // Prefer mono when both coverage and reinforcement scores tie.
            foreach (var candidate in TeamThreats.Candidates().OrderBy(types => types.Sub >= 0))
            {
                int gain = Coverage(team, targets, member, candidate) - baseline;
                int reinforcement = Reinforcement(team, targets, member, candidate);
                if (best == null || gain > best.Gain || gain == best.Gain && reinforcement > best.Reinforcement)
                    best = new Recommendation { Types = candidate, Gain = gain, Reinforcement = reinforcement, Counter = counter };
            }
            if (best != null)
                best.Answers = targets.Select(target => new Answer
                {
                    Main = target.Main,
                    Sub = target.Sub,
                    Attackers = team.Count(ally => !ReferenceEquals(ally, member) && ally.Known && TeamPlan.Damage(ally, target) > 1.0001),
                    Defenders = team.Count(ally => !ReferenceEquals(ally, member) && ally.Known && TeamPlan.Damage(target, ally) < .9999),
                    Dealt = TeamPlan.Damage(best.Types, target),
                    Received = TeamPlan.Damage(target, best.Types)
                }).ToArray();
            return best;
        }
        // Reward independent backup answers, with diminishing returns so poorly
        // supported targets take priority over piling answers onto the same target.
        private static int Reinforcement(TeamMember[] team, TeamMember[] targets, TeamMember replaced, TeamMember candidate)
        {
            int score = 0;
            foreach (var target in targets)
            {
                int attackers = 0, defenders = 0;
                foreach (var ally in team)
                {
                    if (ReferenceEquals(ally, replaced) || !ally.Known)
                        continue;
                    if (TeamPlan.Damage(ally, target) > 1.0001)
                        attackers++;
                    if (TeamPlan.Damage(target, ally) < .9999)
                        defenders++;
                }
                if (TeamPlan.Damage(candidate, target) > 1.0001)
                    score += 120 / (attackers + 1);
                if (TeamPlan.Damage(target, candidate) < .9999)
                    score += 60 / (defenders + 1);
            }
            // Equal coverage and backups prefer a distinct elemental profile.
            return score * 7 - team.Count(ally => ally.Known && !ReferenceEquals(ally, replaced)
                && (ally.Main == candidate.Main && ally.Sub == candidate.Sub || ally.Main == candidate.Sub && ally.Sub == candidate.Main));
        }
        // A transparent heuristic: one point per threat with a resistant answer,
        // two per target with a super-effective answer. No species/stats assumed.
        private static int Coverage(TeamMember[] allies, TeamMember[] targets, TeamMember replaced = null, TeamMember candidate = null)
        {
            int score = 0;
            foreach (var target in targets)
            {
                bool strong = false, resistant = false;
                foreach (var original in allies)
                {
                    var ally = ReferenceEquals(original, replaced) ? candidate : original;
                    if (!strong)
                        strong = TeamPlan.Damage(ally, target) > 1.0001;
                    if (!resistant)
                        resistant = TeamPlan.Damage(target, ally) < .9999;
                    if (strong && resistant)
                        break;
                }
                score += (strong ? 2 : 0) + (resistant ? 1 : 0);
            }
            return score;
        }
        private static TeamMember[] Targets(TeamPlan plan, bool enemy)
        {
            var targets = (enemy ? plan.Allies : plan.Opponents).Where(target => target.Known).ToArray();
            return targets.Length > 0 ? targets : Enumerable.Range(0, Model.Names.Length).Select(element => new TeamMember { Main = element }).ToArray();
        }
        internal static int Gain(TeamPlan plan, TeamMember member, int type, bool enemy = false)
        {
            if (type == member.Main || type == member.Sub)
                return 0;
            var team = enemy ? plan.Opponents : plan.Allies;
            var targets = Targets(plan, enemy);
            return Gain(team, targets, member, type, Coverage(team, targets));
        }
        internal static int[] Gains(TeamPlan plan, TeamMember member, bool enemy = false)
        {
            var team = enemy ? plan.Opponents : plan.Allies;
            var targets = Targets(plan, enemy);
            int baseline = Coverage(team, targets);
            var gains = new int[Model.Names.Length];
            for (int type = 0; type < gains.Length; type++)
                gains[type] = Gain(team, targets, member, type, baseline);
            return gains;
        }
        private static int Gain(TeamMember[] team, TeamMember[] targets, TeamMember member, int type, int baseline)
        {
            if (type == member.Main || type == member.Sub)
                return 0;
            var selection = new Selection { Main = member.Main, Sub = member.Sub };
            selection.Toggle(type);
            var candidate = new TeamMember { Main = selection.Main, Sub = selection.Sub };
            return Coverage(team, targets, member, candidate) - baseline;
        }
    }
}
