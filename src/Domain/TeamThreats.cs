using System;
using System.Linq;
using System.Text;

namespace AniimoAffinities
{
    internal static class TeamThreats
    {
        internal static TeamMember[] Candidates()
        {
            return Enumerable.Range(0, 9).SelectMany(main =>
                new[] { new TeamMember { Main = main } }.Concat(
                    Enumerable.Range(main + 1, 8 - main).Select(sub => new TeamMember { Main = main, Sub = sub }))).ToArray();
        }
        private static string Types(TeamMember member)
        {
            return Language.T(Model.Names[member.Main]) + (member.Sub < 0 ? "" : " + " + Language.T(Model.Names[member.Sub]));
        }
        internal static string Describe(TeamPlan plan, bool enemy = false)
        {
            var allies = (enemy ? plan.Opponents : plan.Allies).Select((member, index) => new { Member = member, Name = member.Title(index, enemy) }).Where(item => item.Member.Known).ToArray();
            if (allies.Length == 0)
                return enemy ? Language.T("Choose at least one opponent's elements to see threats and vulnerable members.") : Language.T("Choose at least one ally's elements to see threats and vulnerable allies.");
            var candidates = Candidates();
            var threats = candidates.Select(target => new
            {
                Target = target,
                Weak = allies.Where(ally => TeamPlan.Damage(target, ally.Member) > 1.0001).ToArray(),
                Resistant = allies.Count(ally => TeamPlan.Damage(target, ally.Member) < .9999),
                Maximum = allies.Max(ally => TeamPlan.Damage(target, ally.Member))
            }).Where(threat => threat.Weak.Length > 0).OrderBy(threat => threat.Resistant > 0).ThenByDescending(threat => threat.Weak.Length).ThenByDescending(threat => threat.Maximum).ThenBy(threat => Types(threat.Target)).ToArray();
            var text = new StringBuilder();
            text.AppendLine(Language.T("TYPE THREATS — ") + (enemy ? Language.T("Known opponents") : Language.T("Your team")) + " · " + allies.Length + Language.T(" known members"));
            text.AppendLine(Language.T("9 single types + 36 dual types. Dual threats use their best attacking element, not multiplied damage. Unknown members are excluded."));
            text.AppendLine();
            foreach (bool dual in new[] { false, true })
            {
                text.AppendLine(dual ? Language.T("DUAL-TYPE THREATS") : Language.T("SINGLE-TYPE THREATS"));
                var group = threats.Where(threat => (threat.Target.Sub >= 0) == dual).ToArray();
                if (group.Length == 0)
                    text.AppendLine(Language.T("No super-effective threat."));
                foreach (var threat in group)
                {
                    text.AppendLine(Types(threat.Target) + " · " + threat.Weak.Length + "/" + allies.Length + Language.T(" vulnerable · ") + threat.Resistant + Language.T(" resistant · max ") + Model.Factor(threat.Maximum));
                    text.AppendLine("  Exposed: " + TeamMember.JoinNames(threat.Weak.Select(ally => ally.Name).ToArray(), threat.Weak.Select(ally => ally.Member).ToArray()) + (threat.Resistant == 0 ? Language.T(" · No resistant member") : ""));
                }
                text.AppendLine();
            }
            var opponents = (enemy ? plan.Allies : plan.Opponents).Where(member => member.Known).ToArray();
            var assessed = opponents.Length > 0 ? opponents : candidates;
            text.AppendLine((enemy ? Language.T("MOST EXPOSED OPPONENTS — ") : Language.T("MOST EXPOSED ALLIES — ")) + (opponents.Length > 0 ? (enemy ? Language.T("against your known allies") : Language.T("against known opponents")) : Language.T("against the 45 type combinations")));
            text.AppendLine(Language.T("Types only: this is not an overall ranking of team members. Ties are equally exposed."));
            foreach (var ally in allies.Select(ally => new
            {
                ally.Name,
                Weak = assessed.Where(target => TeamPlan.Damage(target, ally.Member) > 1.0001).ToArray(),
                Maximum = assessed.Max(target => TeamPlan.Damage(target, ally.Member))
            }).OrderByDescending(ally => ally.Weak.Length).ThenByDescending(ally => ally.Maximum))
            {
                text.AppendLine(ally.Name + Language.T(" · vulnerable to ") + ally.Weak.Length + "/" + assessed.Length + " · max " + Model.Factor(ally.Maximum));
                if (ally.Weak.Length > 0)
                    text.AppendLine("  " + string.Join(", ", ally.Weak.Select(Types)));
            }
            return text.ToString();
        }
    }
}
