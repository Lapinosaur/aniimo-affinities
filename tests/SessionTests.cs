using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class SessionTests
    {
        internal static void Run()
        {
            string sessionTestPath = Path.Combine(Path.GetTempPath(), "aniimo-session-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                var savedSession = new SessionState { Main = Array.IndexOf(Model.Names, "Fire"), Sub = Array.IndexOf(Model.Names, "Dark"), Current = "matrix", Radar = "affinities", Matrix = "radar" };
                savedSession.Save(sessionTestPath);
                var loadedSession = SessionState.Load(sessionTestPath);
                if (loadedSession.Main != savedSession.Main || loadedSession.Sub != savedSession.Sub || loadedSession.Current != "matrix" || loadedSession.Radar != "affinities" || loadedSession.Matrix != "radar")
                    throw new Exception("Session did not round-trip");
                savedSession.Main = -1;
                savedSession.Sub = -1;
                savedSession.Save(sessionTestPath);
                loadedSession = SessionState.Load(sessionTestPath);
                if (loadedSession.Main != -1 || loadedSession.Sub != -1)
                    throw new Exception("Empty selection must persist");
                File.WriteAllLines(sessionTestPath, new[] { "1", "Unknown", "", "radar", "radar", "matrix" });
                loadedSession = SessionState.Load(sessionTestPath);
                if (loadedSession.Current != "affinities" || loadedSession.Main < 0)
                    throw new Exception("Invalid session must fall back safely");
            }
            finally { if (File.Exists(sessionTestPath)) File.Delete(sessionTestPath); }

        }
    }
}
