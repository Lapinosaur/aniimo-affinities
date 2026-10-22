using System;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2 && (args[0] == "--apply-update" || args[0] == "--recover-update"))
                {
                    int parent;
                    if (!int.TryParse(args[1], out parent))
                        return 1;
                    if (args[0] == "--recover-update")
                        ReleaseUpdates.Recover(parent);
                    else
                        ReleaseUpdates.Install(parent);
                    return 0;
                }
                try
                {
                    if (!(args.Length == 2 && args[0] == "--update-ready") && ReleaseUpdates.BeginInstall(Application.ExecutablePath))
                        return 0;
                }
                catch (Exception)
                {
                    MessageBox.Show("The prepared update could not be installed. Your current app will open.", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                ApplicationBootstrap.Initialize();
                Application.ThreadException += (s, e) => { DiagnosticReport.Record(e.Exception); MessageBox.Show("An error occurred. You can copy diagnostics from About & Credits.", "Aniimo Affinities", MessageBoxButtons.OK, MessageBoxIcon.Error); };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { var error = e.ExceptionObject as Exception; if (error != null) DiagnosticReport.Record(error); };
                var main = new MainForm(true);
                main.Shown += (s, e) =>
                {
                    UpdatePrompt.OnStartup(main);
                    if (args.Length == 2 && args[0] == "--update-ready")
                    {
                        var startup = new Timer { Interval = 5000 };
                        main.Disposed += (a, b) => startup.Dispose();
                        startup.Tick += (a, b) => { startup.Stop(); startup.Dispose(); ReleaseUpdates.ConfirmStartup(args[1]); };
                        startup.Start();
                    }
                };
                ReleaseUpdates.Cleanup();
                Application.Run(main);
                return 0;
            }
            catch (Exception error)
            {
                DiagnosticReport.Record(error);
                MessageBox.Show(error.Message, "Aniimo Affinities", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
