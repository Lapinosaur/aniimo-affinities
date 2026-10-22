using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace AniimoAffinities
{
    internal sealed class ReleaseChange
    {
        internal string Version, Notes;
    }
    internal static partial class ReleaseUpdates
    {
        private static void LoadHistory(ReleaseUpdate update)
        {
            var changes = new List<ReleaseChange>(update.Changes);
            try
            {
                // A bounded crawl avoids silently truncating the default GitHub page of 30 releases.
                for (int page = 1; page <= 10; page++)
                {
                    string json = Encoding.UTF8.GetString(Fetch("https://api.github.com/repos/Lapinosaur/aniimo-affinities/releases?per_page=100&page=" + page, 2 * 1024 * 1024));
                    int count;
                    changes.AddRange(ParseHistory(json, CurrentVersion, update.Version, out count));
                    if (count < 100)
                    {
                        update.HistoryComplete = true;
                        break;
                    }
                }
            }
            catch (System.Net.WebException) { }
            catch (System.IO.IOException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            update.Changes = changes.GroupBy(change => new Version(change.Version.TrimStart('v'))).Select(group => group.First()).OrderByDescending(change => new Version(change.Version.TrimStart('v'))).ToArray();
        }
        internal static ReleaseChange[] ParseHistory(string json, string installed, string latest, out int count)
        {
            var releases = new JavaScriptSerializer().Deserialize<Dictionary<string, object>[]>(json);
            count = releases.Length;
            var result = new List<ReleaseChange>();
            var lower = new Version(installed.TrimStart('v'));
            var upper = new Version(latest.TrimStart('v'));
            foreach (var release in releases)
            {
                object tag, draft, prerelease, body;
                Version version;
                if (!release.TryGetValue("tag_name", out tag) || !(tag is string) || !Version.TryParse(((string)tag).TrimStart('v'), out version) || version <= lower || version > upper)
                    continue;
                if (!release.TryGetValue("draft", out draft) || !(draft is bool) || (bool)draft || !release.TryGetValue("prerelease", out prerelease) || !(prerelease is bool) || (bool)prerelease)
                    continue;
                string notes = release.TryGetValue("body", out body) ? body as string : null;
                result.Add(new ReleaseChange { Version = (string)tag, Notes = notes ?? "" });
            }
            return result.OrderByDescending(change => new Version(change.Version.TrimStart('v'))).ToArray();
        }
    }
}
