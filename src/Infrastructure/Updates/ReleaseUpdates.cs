using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace AniimoAffinities
{
    internal sealed class ReleaseUpdate
    {
        internal string Version, Download, Checksums;
    }
    internal static partial class ReleaseUpdates
    {
        internal static string CurrentVersion
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            }
        }
        private const string Repository = "https://github.com/Lapinosaur/aniimo-affinities/releases/download/";
        private static string Folder
        {
            get
            {
                return PreferenceFiles.FilePath("updates");
            }
        }
        private static string Pending
        {
            get
            {
                return Path.Combine(Folder, "pending.txt");
            }
        }
        internal static bool IsNewer(string tag)
        {
            Version version;
            return Version.TryParse(tag.TrimStart('v'), out version) && version > new Version(CurrentVersion);
        }
        internal static ReleaseUpdate Check()
        {
            string json = Encoding.UTF8.GetString(Fetch("https://api.github.com/repos/Lapinosaur/aniimo-affinities/releases/latest", 2 * 1024 * 1024));
            var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            string tag = (string)data["tag_name"];
            if ((bool)data["draft"] || (bool)data["prerelease"] || !IsNewer(tag))
                return null;
            var result = new ReleaseUpdate { Version = tag };
            foreach (var item in (System.Collections.IEnumerable)data["assets"])
            {
                var asset = (Dictionary<string, object>)item;
                string name = (string)asset["name"], url = (string)asset["browser_download_url"];
                if (!url.StartsWith(Repository + Uri.EscapeDataString(tag) + "/", StringComparison.Ordinal))
                    continue;
                if (name == "AniimoAffinities.exe")
                    result.Download = url;
                if (name == "SHA256SUMS.txt")
                    result.Checksums = url;
            }
            if (result.Download == null || result.Checksums == null)
                throw new InvalidDataException("Release assets are incomplete.");
            return result;
        }
        private static byte[] Fetch(string url, int limit, Action<int> progress = null, System.Threading.CancellationToken cancellation = default(System.Threading.CancellationToken))
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "AniimoAffinities/" + CurrentVersion;
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            using (cancellation.Register(request.Abort))
            using (var response = request.GetResponse())
            using (var input = response.GetResponseStream())
            using (var output = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (output.Length + count > limit)
                        throw new InvalidDataException("Download exceeds the size limit.");
                    cancellation.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, count);
                    if (progress != null)
                        progress(response.ContentLength > 0 ? (int)(output.Length * 100 / response.ContentLength) : 0);
                }
                return output.ToArray();
            }
        }
        internal static string Checksum(string text)
        {
            foreach (string line in text.Split('\n'))
            {
                string[] parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[1].TrimStart('*') == "AniimoAffinities.exe" && parts[0].Length == 64 && parts[0].All(Uri.IsHexDigit))
                    return parts[0];
            }
            throw new InvalidDataException("Release checksum is missing.");
        }
        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
        internal static void Prepare(ReleaseUpdate update, string target, Action<int> progress = null, System.Threading.CancellationToken cancellation = default(System.Threading.CancellationToken))
        {
            string expected = Checksum(Encoding.UTF8.GetString(Fetch(update.Checksums, 65536, null, cancellation)));
            byte[] bytes = Fetch(update.Download, 64 * 1024 * 1024, progress, cancellation);
            cancellation.ThrowIfCancellationRequested();
            Stage(bytes, expected, update.Version, target);
        }
        internal static void Stage(byte[] bytes, string expected, string version, string target)
        {
            if (!IsNewer(version))
                throw new InvalidDataException("Update must be newer than the current version.");
            if (!string.Equals(Hash(bytes), expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Checksum mismatch.");
            Directory.CreateDirectory(Folder);
            using (var updateLock = Lock())
            {
                string payload = Path.Combine(Folder, Guid.NewGuid().ToString("N") + ".exe");
                File.WriteAllBytes(payload, bytes);
                PreferenceFiles.WriteLines(Pending, new[] { Path.GetFullPath(target), payload, expected, version });
            }
        }
        private static void Validate(string[] plan)
        {
            if (plan.Length != 4 || !IsNewer(plan[3]) || !Path.IsPathRooted(plan[0]) || !plan[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(plan[1])), Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid update plan.");
            if (!string.Equals(Hash(File.ReadAllBytes(plan[1])), plan[2], StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update verification failed.");
        }
    }
}
