using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class ResourceTests
    {
        internal static void Run()
        {
            foreach (string unsafeXml in new[] { "<!DOCTYPE svg [<!ENTITY test 'expanded'>]><svg>&test;</svg>", "<svg>" + new string('x', 1024 * 1024) + "</svg>" })
            {
                bool rejected = false;
                using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(unsafeXml)))
                    try
                    {
                        VectorIcon.ReadDocument(stream);
                    }
                    catch (System.Xml.XmlException) { rejected = true; }
                if (!rejected)
                    throw new Exception("SVG XML must reject DTDs and oversized documents.");
            }
            if (NativeMethods.TrySetWindowStyle(IntPtr.Zero, 0))
                throw new Exception("Invalid window handles must report style update failure.");
            using (var tips = ApplicationTips.Create())
            {
                var button = new Button();
                tips.SetToolTip(button, "Test");
                button.Dispose();
                var labels = (IDictionary)typeof(ApplicationTips).GetField("labels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tips);
                if (labels.Count != 0)
                    throw new Exception("Tooltip retains a disposed control");
            }
            var control = new Label();
            var first = new Font("Segoe UI", 10);
            var second = new Font("Segoe UI", 12);
            UiFonts.Assign(control, first);
            UiFonts.Assign(control, second);
            AssertDisposed(first);
            control.Dispose();
            AssertDisposed(second);

            float scale = DisplaySettings.FontScale, zoom = DisplaySettings.WindowScale;
            bool badges = DisplaySettings.ShowBadges;
            try
            {
                using (var form = new Form())
                using (var child = new Label())
                {
                    UiFonts.Assign(form, new Font("Segoe UI", 10));
                    form.Controls.Add(child);
                    form.Show();
                    var original = form.Font;
                    DisplaySettings.ApplyDisplay(scale, badges, zoom);
                    DisplaySettings.ApplyDisplay(scale, !badges, zoom);
                    if (!ReferenceEquals(form.Font, original) || !ReferenceEquals(child.Font, original))
                        throw new Exception("Badge-only update recreated fonts");
                    DisplaySettings.ApplyDisplay(scale * 1.1f, badges, zoom);
                    if (Math.Abs(form.Font.Size - 11) > .01 || Math.Abs(child.Font.Size - 11) > .01)
                        throw new Exception("Inherited font was not scaled exactly once");
                    DisplaySettings.ApplyDisplay(scale, badges, zoom);
                    if (Math.Abs(child.Font.Size - 10) > .01)
                        throw new Exception("Font rollback drift");
                }
            }
            finally { DisplaySettings.ApplyDisplay(scale, badges, zoom); }
        }
        private static void AssertDisposed(Font font)
        {
            try
            {
                font.GetHeight(96);
            }
            catch (ArgumentException) { return; }
            throw new Exception("Owned font was not disposed");
        }
    }
}
