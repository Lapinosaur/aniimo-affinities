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
            if (ReleaseUpdates.IsNewer("v0.1.0") || ReleaseUpdates.IsNewer("v0.3.0") || ReleaseUpdates.IsNewer("v1.0.0-beta") || !ReleaseUpdates.IsNewer("v0.4.0"))
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
                string target = Path.Combine(folder, "App with spaces.exe");
                File.WriteAllText(target, "original fixture");
                bool rejected = false;
                try
                {
                    ReleaseUpdates.Stage(payload, new string('0', 64), "v0.4.0", target);
                }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected || File.Exists(Path.Combine(folder, "updates", "pending.txt")))
                    throw new Exception("Unverified update was staged.");
                ReleaseUpdates.Stage(payload, hash, "v0.4.0", target);
                try
                {
                    ReleaseUpdates.Install(int.MaxValue, path => { throw new IOException("Simulated launch failure"); });
                }
                catch (IOException) { }
                if (File.ReadAllText(target) != "original fixture")
                    throw new Exception("Failed installation must restore the original.");
                ReleaseUpdates.Stage(payload, hash, "v0.4.0", target);
                string launched = null;
                ReleaseUpdates.Install(int.MaxValue, path => launched = path);
                if (launched != target || File.ReadAllText(target) != "update fixture" || File.Exists(Path.Combine(folder, "updates", "pending.txt")))
                    throw new Exception("Prepared update was not installed correctly.");
            }
            finally
            {
                PreferenceFiles.Root = root;
                Directory.Delete(folder, true);
            }
        }
    }
}
