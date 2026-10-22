using System;
using System.IO;

namespace AniimoAffinities
{
    internal static class DiagnosticReport
    {
        internal static void Record(Exception error)
        {
            try
            {
                PreferenceFiles.WriteText(PreferenceFiles.FilePath("last-error.txt"), DateTime.UtcNow.ToString("u") + " · " + error.GetType().Name + " · " + error.HResult.ToString("X8"));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        internal static string Create()
        {
            string last = "None recorded";
            try
            {
                last = File.ReadAllText(PreferenceFiles.FilePath("last-error.txt"));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return "Affinisaur " + ReleaseUpdates.CurrentVersion + "\r\nWindows: " + Environment.OSVersion.VersionString + "\r\n64-bit OS: " + Environment.Is64BitOperatingSystem + "\r\n.NET: " + Environment.Version + "\r\nWindow scale: " + DisplaySettings.WindowScale + "\r\nText scale: " + DisplaySettings.FontScale + "\r\nAppearance (0 dark / 1 system / 2 light): " + Theme.Appearance + "\r\nLast error: " + last;
        }
    }
}
