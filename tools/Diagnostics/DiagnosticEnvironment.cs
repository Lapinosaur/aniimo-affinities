using System;
using System.IO;
namespace AniimoAffinities
{
    internal sealed class DiagnosticEnvironment : IDisposable
    {
        private readonly string previous = PreferenceFiles.Root;
        private readonly string temporary = Path.Combine(Path.GetTempPath(), "aniimo-diagnostics-" + Guid.NewGuid().ToString("N"));
        internal DiagnosticEnvironment()
        {
            PreferenceFiles.Root = temporary;
        }
        public void Dispose()
        {
            PreferenceFiles.Root = previous;
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, true);
        }
    }
}
