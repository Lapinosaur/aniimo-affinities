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
            var current = new Version(ReleaseUpdates.CurrentVersion);
            string future = "v" + new Version(current.Major + 1, 0, 0);
            if (ReleaseUpdates.IsNewer("v0.0.0") || ReleaseUpdates.IsNewer("v" + ReleaseUpdates.CurrentVersion) || ReleaseUpdates.IsNewer(future + "-beta") || !ReleaseUpdates.IsNewer(future))
                throw new Exception("Release version comparison failed.");
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
