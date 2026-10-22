using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AniimoAffinities
{
    internal static class UpdateTests
    {
        internal static void Run()
        {
            int releaseCount;
            string history = "[{\"tag_name\":\"v0.9.0\",\"draft\":false,\"prerelease\":false,\"body\":\"Older fixes\"},{\"tag_name\":\"v0.10.0\",\"draft\":false,\"prerelease\":false,\"body\":null},{\"tag_name\":\"v0.8.0\",\"draft\":false,\"prerelease\":false},{\"tag_name\":\"v0.11.0\",\"draft\":false,\"prerelease\":false},{\"tag_name\":\"v0.9.1\",\"draft\":true,\"prerelease\":false},{\"tag_name\":\"v0.9.2\",\"draft\":false,\"prerelease\":true}]";
            var changes = ReleaseUpdates.ParseHistory(history, "0.8.0", "v0.10.0", out releaseCount);
            if (releaseCount != 6 || changes.Length != 2 || changes[0].Version != "v0.10.0" || changes[1].Version != "v0.9.0" || changes[0].Notes != "")
                throw new Exception("History must include only stable missing versions, ordered numerically, including versions without notes.");
            var description = UpdateHistoryView.Describe(new ReleaseUpdate { Version = "v0.10.0", Changes = changes, HistoryComplete = true });
            if (!description.Contains("2 newer versions") || !description.Contains("Older fixes") || !description.Contains("No release notes"))
                throw new Exception("Update recap omitted versions or missing-note feedback.");
            description = UpdateHistoryView.Describe(new ReleaseUpdate { Changes = new[] { new ReleaseChange { Version = "v1", Notes = "## Fixes\n<!-- internal marker -->\n- A fix" } } });
            if (description.Contains("internal marker") || !description.Contains("incomplete or unavailable") || !description.Contains("A fix"))
                throw new Exception("History fallback must be explicit and hide internal comments.");
            var current = new Version(ReleaseUpdates.CurrentVersion);
            string future = "v" + new Version(current.Major + 1, 0, 0);
            if (ReleaseUpdates.IsNewer("v0.0.0") || ReleaseUpdates.IsNewer("v" + ReleaseUpdates.CurrentVersion) || ReleaseUpdates.IsNewer(future + "-beta") || !ReleaseUpdates.IsNewer(future))
                throw new Exception("Release version comparison failed.");
            string manifest = new string('a', 64) + "  Affinisaur.exe\n" + new string('b', 64) + "  AniimoAffinities.exe\n";
            if (ReleaseUpdates.Checksum(manifest, "Affinisaur.exe") != new string('a', 64) || ReleaseUpdates.Checksum(manifest) != new string('b', 64))
                throw new Exception("Checksums must match the selected asset, not just the first manifest entry.");
            foreach (bool reverse in new[] { false, true })
            {
                string newer = "{\"name\":\"Affinisaur.exe\",\"browser_download_url\":\"https://github.com/Lapinosaur/aniimo-affinities/releases/download/" + future + "/Affinisaur.exe\"}";
                string legacy = newer.Replace("Affinisaur.exe", "AniimoAffinities.exe");
                string checksums = newer.Replace("Affinisaur.exe", "SHA256SUMS.txt");
                string json = "{\"tag_name\":\"" + future + "\",\"draft\":false,\"prerelease\":false,\"assets\":[" + (reverse ? legacy + "," + newer : newer + "," + legacy) + "," + checksums + "]}";
                if (ReleaseUpdates.ParseRelease(json).AssetName != "Affinisaur.exe")
                    throw new Exception("The new executable must be preferred regardless of asset order.");
                json = "{\"tag_name\":\"" + future + "\",\"draft\":false,\"prerelease\":false,\"assets\":[" + legacy + "," + checksums + "]}";
                if (ReleaseUpdates.ParseRelease(json).AssetName != "AniimoAffinities.exe")
                    throw new Exception("Existing releases must remain downloadable.");
            }
            byte[] payload = Encoding.UTF8.GetBytes("update fixture");
            string hash;
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "");
            if (ReleaseUpdates.Checksum(hash + "  AniimoAffinities.exe\n") != hash)
                throw new Exception("Checksum parsing failed.");
            string root = PreferenceFiles.Root;
            string folder = Path.Combine(Path.GetTempPath(), "aniimo-update-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                PreferenceFiles.Root = folder;
                string tutorialPath = Path.Combine(folder, "tutorial.txt");
                TutorialPreferences.Save(true, tutorialPath);
                string target = Path.Combine(folder, "App with spaces.exe");
                File.WriteAllText(target, "original fixture");
                bool rejected = false;
                try
                {
                    ReleaseUpdates.Stage(payload, new string('0', 64), future, target);
                }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected || File.Exists(Path.Combine(folder, "updates", "pending.txt")))
                    throw new Exception("Unverified update was staged.");
                ReleaseUpdates.Stage(payload, hash, future, target);
                try
                {
                    ReleaseUpdates.Install(int.MaxValue, path => { throw new IOException("Simulated launch failure"); });
                }
                catch (IOException) { }
                if (File.ReadAllText(target) != "original fixture")
                    throw new Exception("Failed installation must restore the original.");
                ReleaseUpdates.Stage(payload, hash, future, target);
                ReleaseUpdates.ClearCache(); // A pending payload must survive cleanup.
                string launched = null;
                ReleaseUpdates.Install(int.MaxValue, path => launched = path);
                if (launched != target || File.ReadAllText(target) != "update fixture" || File.Exists(Path.Combine(folder, "updates", "pending.txt")))
                    throw new Exception("Prepared update was not installed correctly.");
                File.WriteAllText(target, "interrupted update");
                PreferenceFiles.WriteLines(Path.Combine(folder, "updates", "installing.txt"), new[] { target, target + ".previous" });
                ReleaseUpdates.Recover(int.MaxValue, path => launched = path);
                if (File.ReadAllText(target) != "original fixture")
                    throw new Exception("Recovery must restore the previous executable.");
                using (var held = new FileStream(Path.Combine(folder, "updates", "update.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    bool locked = false;
                    try
                    {
                        ReleaseUpdates.Stage(payload, hash, future, target);
                    }
                    catch (IOException) { locked = true; }
                    if (!locked)
                        throw new Exception("Concurrent update mutation was not blocked.");
                }
                File.WriteAllText(Path.Combine(folder, "updates", "installer-old.exe"), "cache");
                File.WriteAllText(Path.Combine(folder, "last-error.txt"), "diagnostic");
                ReleaseUpdates.ClearCache();
                if (File.Exists(Path.Combine(folder, "updates", "installer-old.exe")) || File.Exists(Path.Combine(folder, "last-error.txt")))
                    throw new Exception("Cache cleanup did not remove temporary files.");
                if (!File.Exists(target) || !File.Exists(target + ".previous"))
                    throw new Exception("Cache cleanup deleted an app executable.");
                if (!TutorialPreferences.Load(tutorialPath))
                    throw new Exception("Update reset the tutorial preference.");
            }
            finally
            {
                PreferenceFiles.Root = root;
                Directory.Delete(folder, true);
            }
        }
    }
}
