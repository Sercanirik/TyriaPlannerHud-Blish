using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TyriaPlanner.Hud.Ui
{
    // Replaces the default auto-generated settings panel with a branded
    // header (logo + clickable tyriaplanner.com link) on top of the same
    // auto-generated settings the user is used to. Clicking the logo or
    // link opens the website in the user's default browser.
    public sealed class BrandedSettingsView : View
    {
        private const string SiteUrl = "https://tyriaplanner.com";
        private const int HeaderHeight = 80;

        private readonly SettingCollection _settings;
        private readonly Texture2D _icon;

        public BrandedSettingsView(SettingCollection settings, Texture2D icon)
        {
            _settings = settings;
            _icon = icon;
        }

        protected override void Build(Container buildPanel)
        {
            // Header strip with logo + link.
            var header = new Panel
            {
                Parent = buildPanel,
                Location = new Point(0, 0),
                Width = buildPanel.ContentRegion.Width,
                Height = HeaderHeight,
                BackgroundColor = new Color(20, 20, 26, 255),
            };

            if (_icon != null)
            {
                var logo = new Image
                {
                    Parent = header,
                    Texture = _icon,
                    Location = new Point(12, 12),
                    Size = new Point(56, 56),
                };
                logo.Click += (_, __) => SafeUrl.Open(SiteUrl);
            }

            var title = new Label
            {
                Parent = header,
                Text = "Tyria Planner",
                Font = Blish_HUD.GameService.Content.DefaultFont18,
                TextColor = Color.Goldenrod,
                Location = new Point(80, 14),
                Width = header.Width - 92,
                Height = 28,
                AutoSizeWidth = false,
            };
            title.Click += (_, __) => SafeUrl.Open(SiteUrl);

            var link = new Label
            {
                Parent = header,
                Text = "tyriaplanner.com  ·  click to open",
                Font = Blish_HUD.GameService.Content.DefaultFont14,
                TextColor = new Color(180, 200, 255),
                Location = new Point(80, 42),
                Width = header.Width - 92,
                Height = 22,
                AutoSizeWidth = false,
            };
            link.Click += (_, __) => SafeUrl.Open(SiteUrl);

            // Auto-generated settings panel below the header · same controls
            // the user would have seen without this customisation, just
            // shifted down to make room for the branding.
            var settingsHost = new ViewContainer
            {
                Parent = buildPanel,
                Location = new Point(0, HeaderHeight + 4),
                Size = new Point(
                    buildPanel.ContentRegion.Width,
                    buildPanel.ContentRegion.Height - HeaderHeight - 4),
            };
            settingsHost.Show(new Blish_HUD.Settings.UI.Views.SettingsView(_settings));
        }
    }
}
