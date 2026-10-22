using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AniimoAffinities
{
    internal static class TeamPreferences
    {
        internal static string FilePath
        {
            get
            {
                return PreferenceFiles.FilePath("teams.txt");
            }
        }
        internal static TeamPlan Load(string path)
        {
            var plan = new TeamPlan();
            try
            {
                var lines = File.ReadAllLines(path);
                if (lines.Length != 15 || lines[0] != "1")
                    return plan;
                var candidate = new TeamPlan();
                if (!new[] { "Top", "Bottom", "Left", "Right" }.Contains(lines[1]))
                    return plan;
                candidate.Position = lines[1];
                if (!bool.TryParse(lines[2], out candidate.Visible))
                    return plan;
                var members = candidate.Allies.Concat(candidate.Opponents).ToArray();
                for (int i = 0; i < members.Length; i++)
                {
                    var parts = lines[i + 3].Split('|');
                    int main, sub;
                    if (parts.Length != 3 || !int.TryParse(parts[1], out main) || !int.TryParse(parts[2], out sub)
                        || main < -1 || main >= 9 || sub < -1 || sub >= 9 || (main < 0 && sub >= 0) || (main >= 0 && main == sub))
                        return plan;
                    string name = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                    if (name.Length > 40 || name.Any(char.IsControl))
                        return plan;
                    members[i].Name = name;
                    members[i].Main = main;
                    members[i].Sub = sub;
                }
                return candidate;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            return plan;
        }
        internal static void Save(string path, TeamPlan plan)
        {
            var lines = new[] { "1", plan.Position, plan.Visible.ToString() }.Concat(plan.Allies.Concat(plan.Opponents).Select(m =>
                Convert.ToBase64String(Encoding.UTF8.GetBytes(m.Name)) + "|" + m.Main + "|" + m.Sub)).ToArray();
            PreferenceFiles.WriteLines(path, lines);
        }
    }
}
