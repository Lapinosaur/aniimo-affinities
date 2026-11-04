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
            Language.Set(LanguagePreferences.Load());
            Theme.LoadAppearance();
            Application.AddMessageFilter(new WindowZoomWheel());
            Application.AddMessageFilter(new SupportShortcut());
        }
    }
}
