using System;
using System.IO;
using System.Reflection;
namespace AniimoAffinities
{
    internal static class PackagingTests
    {
        internal static void Run()
        {
            string executable = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "AniimoAffinities.exe"));
            var runtime = Assembly.LoadFile(executable);
            if (runtime.GetType("AniimoAffinities.RegressionTests") != null || runtime.GetType("AniimoAffinities.DiagnosticsProgram") != null)
                throw new Exception("Developer tooling must not ship in the runtime executable.");
            if (runtime.GetType("AniimoAffinities.MainForm") == null)
                throw new Exception("Runtime entry view is missing.");
            if (!PreferenceFiles.Root.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
                throw new Exception("Diagnostics must use temporary preferences.");
        }
    }
}
