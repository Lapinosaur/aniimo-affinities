using System;
using System.Diagnostics;
using System.IO;

namespace AniimoAffinities
{
    internal static class FactoryResetTests
    {
        internal static void Run()
        {
            string scope = Path.Combine(Path.GetTempPath(), "affinisaur-reset-test-" + Guid.NewGuid().ToString("N"));
            string root = Path.Combine(scope, "profile");
            Directory.CreateDirectory(root);
            try
            {
                string team = Path.Combine(root, "teams.txt");
                string marker = Path.Combine(root, "reset-requested.txt");
                File.WriteAllText(team, "keep this team in backup");
                if (FactoryReset.ApplyPending(root) != null || !File.Exists(team))
                    throw new Exception("A normal restart must preserve the profile.");
                foreach (string invalid in new[] { "invalid", "-1", Process.GetCurrentProcess().Id.ToString() })
                {
                    File.WriteAllText(marker, invalid);
                    bool rejected = false;
                    try
                    {
                        FactoryReset.ApplyPending(root);
                    }
                    catch (IOException) { rejected = true; }
                    if (!rejected || !File.Exists(team))
                        throw new Exception("Invalid or still-running reset requests must preserve data.");
                }
                File.WriteAllText(Path.Combine(root, "onboarding.txt"), "complete-v1\nen");
                Directory.CreateDirectory(Path.Combine(root, "nested"));
                File.WriteAllText(Path.Combine(root, "nested", "preferences.txt"), "original");
                File.WriteAllText(marker, "0");
                string backup = FactoryReset.ApplyPending(root);
                if (Directory.GetFileSystemEntries(root).Length != 0 || !OnboardingPreferences.ShouldShow(root))
                    throw new Exception("Reset must create an empty first-launch profile.");
                if (File.ReadAllText(Path.Combine(backup, "teams.txt")) != "keep this team in backup" || File.ReadAllText(Path.Combine(backup, "nested", "preferences.txt")) != "original")
                    throw new Exception("Reset must retain all original profile files in its backup.");
                if (FactoryReset.ApplyPending(root) != null)
                    throw new Exception("Reset must run only once.");
            }
            finally { Directory.Delete(scope, true); }
        }
    }
}
