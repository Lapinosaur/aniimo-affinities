using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed partial class TutorialOverlay
    {
        private Frame CaptureUpdatePreview(string view)
        {
            // Capture real controls without invoking any download, skip or install action.
            var update = new ReleaseUpdate
            {
                Version = Language.T("Demo"),
                HistoryComplete = true,
                Changes = new[] { new ReleaseChange { Version = Language.T("Demo"), Notes = Language.T("## Example release notes\n- **New features** and interface improvements.\n- Fixes and performance improvements.\n\n| Step | What happens |\n| --- | --- |\n| Download | Prepares the update |\n| Restart app | Installs and relaunches |\n\nThese notes are a tutorial example, not an available release.") } }
            };
            using (var host = new UpdateWindow(update) { TutorialSnapshot = true })
            {
                host.Opacity = 0;
                host.ShowInTaskbar = false;
                host.StartPosition = FormStartPosition.Manual;
                host.Location = Location;
                host.Show();
                Application.DoEvents();
                if (view == "update-install")
                {
                    host.Controls.OfType<SettingsButton>().Single(button => button.Text == Language.T("Download")).Text = Language.T("Restart app");
                    host.Controls.OfType<SettingsButton>().Single(button => button.Text == Language.T("Skip this version")).Visible = false;
                    host.Controls.OfType<SettingsButton>().Single(button => button.Text == Language.T("Remind me later")).Text = Language.T("Later");
                    host.Controls.OfType<Label>().Single(label => label.Name == "updateStatus").Text = Language.T("Demo: update ready. Restart Affinisaur to apply it. Your PC stays on.");
                }
                var frame = new Frame { Image = new Bitmap(host.Width, host.Height) };
                host.DrawToBitmap(frame.Image, new Rectangle(Point.Empty, host.Size));
                frame.Targets["all"] = new Rectangle(8, 40, host.Width - 16, host.Height - 48);
                frame.Targets["notes"] = new Rectangle(24, 136, 512, 342);
                frame.Targets["choices"] = new Rectangle(24, 516, 280, 44);
                frame.Targets["install"] = new Rectangle(312, 516, 224, 44);
                return frame;
            }
        }
    }
}
