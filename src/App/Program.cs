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
                ApplicationBootstrap.Initialize();
                Application.Run(new MainForm(true));
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
