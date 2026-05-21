using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using TyriaPlanner.Hud.Settings;
namespace TyriaPlanner.Hud.Ui
{
    public sealed class AnnouncementToast : Container
    {
        public AnnouncementToast(ModuleSettings settings, string title, string subtitle, string body)
        {
            var titleFont = settings.TitleFont();
            var bodyFont  = settings.BodyFont();
            int titleH    = (int)titleFont.LineHeight;
            int bodyH     = (int)bodyFont.LineHeight;
            int bodyBlock = bodyH * 3 + 4;
            int height    = 12 + titleH + 4 + bodyH + 6 + bodyBlock + 12;
            Height = height;
            BackgroundColor = new Color(14, 14, 18, 235);
            var accent = Color.Goldenrod;
            new Panel
            {
                Parent = this,
                BackgroundColor = accent,
                Location = new Point(0, 0),
                Width = 4,
                Height = height,
            };
            new Label
            {
                Parent = this,
                Text = title,
                Font = titleFont,
                TextColor = accent,
                Location = new Point(12, 8),
                Width = 320,
                Height = titleH + 4,
                AutoSizeWidth = false,
            };
            var dismiss = new StandardButton
            {
                Parent = this,
                Text = "X",
                Width = 30,
                Height = 22,
                Location = new Point(348, 6),
            };
            dismiss.Click += (_, __) => Dispose();
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                new Label
                {
                    Parent = this,
                    Text = subtitle,
                    Font = bodyFont,
                    TextColor = new Color(230, 220, 180),
                    Location = new Point(12, 12 + titleH),
                    Width = 360,
                    Height = bodyH + 4,
                    AutoSizeWidth = false,
                };
            }
            new Label
            {
                Parent = this,
                Text = string.IsNullOrWhiteSpace(body) ? "(no content)" : body,
                Font = bodyFont,
                TextColor = new Color(220, 220, 220),
                Location = new Point(12, 12 + titleH + 4 + bodyH + 4),
                Width = 360,
                Height = bodyBlock,
                AutoSizeWidth = false,
                WrapText = true,
            };
        }
    }
}
