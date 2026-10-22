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
    }

    internal sealed class TeamPlan
    {
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
            var known = Opponents.Where(x => x.Known).ToArray();
            if (!member.Known)
                return "Choose elements";
            if (known.Length == 0)
                return "No known opponents";
            int coverage = known.Count(x => Damage(member, x) > 1.0001);
            int threats = known.Count(x => Damage(x, member) > 1.0001);
            return "Strong vs " + coverage + "/" + known.Length + " · Weak vs " + threats + "/" + known.Length;
        }
    }
}
