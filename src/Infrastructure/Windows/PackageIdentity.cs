using System;
using System.Runtime.InteropServices;

namespace AniimoAffinities
{
    internal static class PackageIdentity
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFullName(ref uint length, System.Text.StringBuilder name);
        internal static readonly bool IsPackaged = Detect();
        private static bool Detect()
        {
            try
            {
                uint length = 0;
                return GetCurrentPackageFullName(ref length, null) != 15700; // APPMODEL_ERROR_NO_PACKAGE
            }
            catch (EntryPointNotFoundException) { return false; }
        }
        internal static void RequirePortableUpdates()
        {
            if (IsPackaged)
                throw new InvalidOperationException(Language.T("Updates for this packaged installation are managed by its distribution channel, not the portable updater."));
        }
    }
}
