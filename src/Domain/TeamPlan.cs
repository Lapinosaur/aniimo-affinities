using System;
using System.Linq;

namespace AniimoAffinities
{
    internal sealed class TeamMember
    {
        internal string Name = "";
        internal int Main = -1, Sub = -1;
        internal bool Known
        {
            get
            {
                return Main >= 0;
            }
        }
        internal string Title(int index, bool opponent = false)
        {
            return string.IsNullOrWhiteSpace(Name) ? (opponent ? "Opponent " : "Ally ") + (index + 1) : Name;
        }
        internal static string JoinNames(string[] names, TeamMember[] members)
        {
            var labels = new string[names.Length];
            string previousPrefix = "";
            for (int i = 0; i < names.Length; i++)
            {
                string prefix = string.IsNullOrWhiteSpace(members[i].Name)
                    ? (names[i].StartsWith("Ally ", StringComparison.Ordinal) ? "Ally " : names[i].StartsWith("Opponent ", StringComparison.Ordinal) ? "Opponent " : "") : "";
                labels[i] = prefix.Length > 0 && prefix == previousPrefix ? names[i].Substring(prefix.Length) : names[i];
                previousPrefix = prefix;
            }
            return string.Join(" & ", labels);
        }
    }

    internal sealed class TeamPlan
    {
        internal struct MatchupCounts
        {
            internal int Total, Strong, Weak;
        }
        internal readonly TeamMember[] Allies = Enumerable.Range(0, 6).Select(i => new TeamMember()).ToArray();
        internal readonly TeamMember[] Opponents = Enumerable.Range(0, 6).Select(i => new TeamMember()).ToArray();
        internal string Position = "Bottom";
        internal bool Visible;

        // Best available attacking element against the target's known elements.
        // Unknown slots never participate. One known element does not imply knowledge of a second.
        internal static double Damage(TeamMember attacker, TeamMember target)
        {
            if (!attacker.Known || !target.Known)
                return double.NaN;
            var incoming = Model.Factors(target.Main, target.Sub);
            return Math.Max(incoming[9 + attacker.Main], attacker.Sub < 0 ? 0 : incoming[9 + attacker.Sub]);
        }
        internal string Summary(TeamMember member)
        {
            if (!member.Known)
                return "Choose elements";
            var counts = CountMatchups(member);
            if (counts.Total == 0)
                return "No known opponents";
            return "Strong vs " + counts.Strong + "/" + counts.Total + " · Weak vs " + counts.Weak + "/" + counts.Total;
        }
        internal MatchupCounts CountMatchups(TeamMember member)
        {
            var result = new MatchupCounts();
            foreach (var opponent in Opponents)
            {
                if (!opponent.Known)
                    continue;
                result.Total++;
                if (Damage(member, opponent) > 1.0001)
                    result.Strong++;
                if (Damage(opponent, member) > 1.0001)
                    result.Weak++;
            }
            return result;
        }
    }
}
