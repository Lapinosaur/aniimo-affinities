using System;
using System.Diagnostics;
using System.IO;

namespace AniimoAffinities
{
    internal static class FactoryReset
    {
        internal static string RequestPath
        {
            get
            {
                return PreferenceFiles.FilePath("reset-requested.txt");
            }
        }
        internal static void Request()
        {
            PreferenceFiles.WriteText(RequestPath, Process.GetCurrentProcess().Id.ToString());
        }
        internal static string ApplyPending(string root = null)
        {
            root = Path.GetFullPath(root ?? PreferenceFiles.Root).TrimEnd(Path.DirectorySeparatorChar);
            string marker = Path.Combine(root, "reset-requested.txt");
            if (!File.Exists(marker))
                return null;
            if (root == Path.GetPathRoot(root).TrimEnd(Path.DirectorySeparatorChar) || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
                throw new IOException(Language.T("The app data location cannot be reset safely."));
            int previous;
            if (!int.TryParse(File.ReadAllText(marker), out previous) || previous < 0)
                throw new IOException(Language.T("The reset request is invalid."));
            if (previous != 0)
            {
                if (previous == Process.GetCurrentProcess().Id)
                    throw new IOException(Language.T("Close the app before resetting its data."));
                try
                {
                    using (var process = Process.GetProcessById(previous))
                        if (!process.WaitForExit(10000))
                            throw new IOException(Language.T("The previous app is still closing. Please try again."));
                }
                catch (ArgumentException) { }
            }
            string backup = root + ".reset-backup-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            // Move the whole profile after shutdown, so pending saves cannot recreate old settings.
            Directory.Move(root, backup);
            Directory.CreateDirectory(root);
            return backup;
        }
    }
}
