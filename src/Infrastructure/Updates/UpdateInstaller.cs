using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AniimoAffinities
{
    internal static partial class ReleaseUpdates
    {
        private static string Journal
        {
            get
            {
                return Path.Combine(Folder, "installing.txt");
            }
        }
        private static FileStream Lock()
        {
            Directory.CreateDirectory(Folder);
            return new FileStream(Path.Combine(Folder, "update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        internal static bool BeginInstall(string target)
        {
            string planPath = File.Exists(Journal) ? Journal : Pending;
            if (!File.Exists(planPath))
                return false;
            using (var guard = Lock())
            {
                var plan = File.ReadAllLines(planPath);
                if (plan.Length == 0 || !string.Equals(plan[0], target, StringComparison.OrdinalIgnoreCase))
                    return false;
                bool recovery = planPath == Journal;
                if (!recovery)
                    Validate(plan);
                string helper = Path.Combine(Folder, "installer-" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(target, helper);
                Process.Start(new ProcessStartInfo(helper, (recovery ? "--recover-update " : "--apply-update ") + Process.GetCurrentProcess().Id) { UseShellExecute = false, CreateNoWindow = true });
                return true;
            }
        }
        private static void WaitForParent(int parentId)
        {
            try
            {
                using (var parent = Process.GetProcessById(parentId))
                    if (!parent.WaitForExit(15000))
                        throw new IOException("Please close the app before updating.");
            }
            catch (ArgumentException) { }
        }
        private static void AtomicCopy(string source, string target, string backup)
        {
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".next";
            try
            {
                File.Copy(source, temporary);
                // The staged file is on the destination volume; replace never truncates the live EXE.
                File.Replace(temporary, target, backup, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static void Install(int parentId, Action<string> launch = null)
        {
            WaitForParent(parentId);
            using (var guard = Lock())
            {
                if (!File.Exists(Pending))
                    return;
                var plan = File.ReadAllLines(Pending);
                Validate(plan);
                string backup = plan[0] + ".previous";
                File.Copy(plan[0], backup, true);
                // A journal survives power loss and redirects the next launch to recovery.
                PreferenceFiles.WriteLines(Journal, new[] { plan[0], backup });
                bool replaced = false;
                try
                {
                    AtomicCopy(plan[1], plan[0], null);
                    replaced = true;
                    File.Delete(Pending);
                    if (launch != null)
                        launch(plan[0]);
                    else
                        LaunchAndVerify(plan[0]);
                    File.Delete(Journal);
                }
                catch
                {
                    if (replaced)
                        AtomicCopy(backup, plan[0], null);
                    File.Delete(Pending);
                    File.Delete(Journal);
                    if (launch == null)
                        Process.Start(new ProcessStartInfo(plan[0]) { UseShellExecute = true });
                    throw;
                }
                TryDelete(plan[1]);
            }
        }
        private static void LaunchAndVerify(string target)
        {
            string token = "Local\\AniimoUpdate-" + Guid.NewGuid().ToString("N");
            using (var ready = new EventWaitHandle(false, EventResetMode.ManualReset, token))
            using (var child = Process.Start(new ProcessStartInfo(target, "--update-ready " + token) { UseShellExecute = false }))
            {
                var watch = Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < 30000)
                {
                    if (ready.WaitOne(100))
                        return;
                    if (child.HasExited)
                        throw new IOException("The updated app exited before confirming startup.");
                }
                if (!child.HasExited)
                {
                    child.Kill();
                    child.WaitForExit(5000);
                }
                throw new IOException("The updated app did not confirm startup.");
            }
        }
        internal static void Recover(int parentId, Action<string> launch = null)
        {
            WaitForParent(parentId);
            using (var guard = Lock())
            {
                if (!File.Exists(Journal))
                    return;
                var plan = File.ReadAllLines(Journal);
                if (plan.Length != 2 || !Path.IsPathRooted(plan[0]) || plan[1] != plan[0] + ".previous")
                    throw new InvalidDataException("Invalid recovery plan.");
                if (File.Exists(plan[1]))
                    AtomicCopy(plan[1], plan[0], null);
                File.Delete(Pending);
                File.Delete(Journal);
                if (launch != null)
                    launch(plan[0]);
                else
                    Process.Start(new ProcessStartInfo(plan[0]) { UseShellExecute = true });
            }
        }
        internal static void ConfirmStartup(string token)
        {
            if (!token.StartsWith("Local\\AniimoUpdate-", StringComparison.Ordinal))
                return;
            try
            {
                using (var ready = EventWaitHandle.OpenExisting(token))
                    ready.Set();
            }
            catch (WaitHandleCannotBeOpenedException) { }
        }
        internal static void Cleanup()
        {
            if (!Directory.Exists(Folder))
                return;
            try
            {
                using (var guard = Lock())
                {
                    if (File.Exists(Journal) || File.Exists(Pending))
                        return;
                    foreach (string file in Directory.GetFiles(Folder, "*.exe"))
                        if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-1))
                            TryDelete(file);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        internal static long ClearCache()
        {
            long removed = 0;
            using (var guard = Lock())
            {
                string retained = null;
                if (File.Exists(Journal))
                    throw new IOException("An update is being installed. Try again after restarting.");
                if (File.Exists(Pending))
                {
                    var plan = File.ReadAllLines(Pending);
                    if (plan.Length != 4)
                        throw new IOException("An update needs recovery before cleanup.");
                    retained = Path.GetFullPath(plan[1]);
                }
                foreach (string path in Directory.GetFiles(Folder, "*.exe"))
                {
                    if (string.Equals(path, retained, StringComparison.OrdinalIgnoreCase))
                        continue;
                    long size = new FileInfo(path).Length;
                    try
                    {
                        File.Delete(path);
                        removed += size;
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                string log = PreferenceFiles.FilePath("last-error.txt");
                if (File.Exists(log))
                {
                    long size = new FileInfo(log).Length;
                    File.Delete(log);
                    removed += size;
                }
            }
            return removed;
        }
        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
