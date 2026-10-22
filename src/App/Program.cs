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
                if (args.Length == 2 && args[0] == "--apply-update")
                {
                    int parent;
                    if (!int.TryParse(args[1], out parent))
                        return 1;
                    ReleaseUpdates.Install(parent);
                    return 0;
                }
                try
                {
                    if (ReleaseUpdates.BeginInstall(Application.ExecutablePath))
                        return 0;
                }
                catch (Exception)
                {
                    MessageBox.Show("The prepared update could not be installed. Your current app will open.", "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                ApplicationBootstrap.Initialize();
                var main = new MainForm(true);
                main.Shown += (s, e) => UpdatePrompt.OnStartup(main);
                Application.Run(main);
                return 0;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Aniimo Affinities", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
