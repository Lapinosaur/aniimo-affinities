using System;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ExternalLinkTips
    {
        internal static string Describe(string url)
        {
            return Language.T("Open in your browser · ") + url;
        }
        internal static void Attach(Control control, string url)
        {
            var tips = ApplicationTips.Create();
            tips.SetToolTip(control, Describe(url));
            control.AccessibleDescription = Describe(url);
            control.Disposed += (s, e) => tips.Dispose();
        }
    }
}
