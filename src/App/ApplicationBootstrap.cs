using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ApplicationBootstrap
    {
        internal static void Initialize()
        {
            NativeMethods.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Theme.LoadAppearance();
        }
    }
}
