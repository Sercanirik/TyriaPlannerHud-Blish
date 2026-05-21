using System;
using System.Diagnostics;
using System.Threading;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using TyriaPlanner.Hud.Settings;
namespace TyriaPlanner.Hud.Ui
{
    public enum ToastAccent
    {
        Reminder,
        NewEvent,
    }
    public sealed class EventToast : Container
    {
        private readonly string _commanderAccountName;
        private readonly string _eventBaseUrl;
        private readonly bool _showSqjoin;
        private readonly bool _showJoinFromAppHint;
        public EventToast(
            ModuleSettings settings,
            string title,
            string subtitle,
            ToastAccent accent,
            string eventType,
            string commanderAccountName,
            string eventId,
            string eventBaseUrl,
            bool showSqjoin = true,
            bool showJoinFromAppHint = false)
        {
            _commanderAccountName = commanderAccountName;
            _eventBaseUrl = eventBaseUrl;
            _showSqjoin = showSqjoin;
            _showJoinFromAppHint = showJoinFromAppHint;
            var titleFont = settings.TitleFont();
            var bodyFont  = settings.BodyFont();
            var baseHeight = showJoinFromAppHint ? 122 : 104;
            Height = baseHeight;
            BackgroundColor = new Color(14, 14, 18, 235);
            var typeColor = EventColors.For(eventType);
            new Panel
            {
                Parent = this,
                BackgroundColor = typeColor,
                Location = new Point(0, 0),
                Width = 4,
                Height = baseHeight,
            };
            new Label
            {
                Parent = this,
                Text = title,
                Font = titleFont,
                TextColor = typeColor,
                Location = new Point(12, 8),
                Width = 320,
                Height = (int)titleFont.LineHeight + 4,
                AutoSizeWidth = false,
            };
            var dismiss = new StandardButton
            {
                Parent = this,
                Text = "X",
                Width = 30,
                Height = 24,
                Location = new Point(348, 6),
            };
            dismiss.Click += (_, __) => Dispose();
            new Label
            {
                Parent = this,
                Text = subtitle,
                Font = bodyFont,
                TextColor = new Color(220, 220, 220),
                Location = new Point(12, 12 + (int)titleFont.LineHeight),
                Width = 360,
                Height = (int)bodyFont.LineHeight + 4,
                WrapText = false,
                AutoSizeWidth = false,
            };
            if (_showJoinFromAppHint)
            {
                new Label
                {
                    Parent = this,
                    Text = "Sign up via the mobile app or website first",
                    Font = bodyFont,
                    TextColor = new Color(255, 200, 90),
                    Location = new Point(12, 14 + (int)titleFont.LineHeight + (int)bodyFont.LineHeight),
                    Width = 360,
                    Height = (int)bodyFont.LineHeight + 4,
                    AutoSizeWidth = false,
                };
            }
            var buttonRowY = baseHeight - 32;
            var x = 12;
            if (_showSqjoin && !string.IsNullOrWhiteSpace(_commanderAccountName))
            {
                AddCopyButton("/sqjoin", $"/sqjoin {_commanderAccountName}", ref x, buttonRowY, 84);
            }
            if (!string.IsNullOrWhiteSpace(_commanderAccountName))
            {
                AddWhisperButton(_commanderAccountName, ref x, buttonRowY, 84);
            }
            AddOpenButton(ref x, buttonRowY);
        }
        private void AddCopyButton(string label, string payload, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = this,
                Text = label,
                Location = new Point(x, y),
                Width = width,
                Height = 26,
            };
            btn.Click += (_, __) =>
            {
                Clipboard.Set(payload);
                FlashCopied(btn, label);
            };
            x += width + 6;
        }
        private void AddWhisperButton(string accountName, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = this,
                Text = "/whisper",
                Location = new Point(x, y),
                Width = width,
                Height = 26,
            };
            btn.Click += (_, __) =>
            {
                _ = WhisperOpener.OpenAsync(accountName);
                FlashCopied(btn, "/whisper");
            };
            x += width + 6;
        }
        private void AddOpenButton(ref int x, int y)
        {
            var open = new StandardButton
            {
                Parent = this,
                Text = "Open",
                Location = new Point(x, y),
                Width = 80,
                Height = 26,
            };
            open.Click += (_, __) =>
            {
                Clipboard.Set(_eventBaseUrl);
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _eventBaseUrl,
                        UseShellExecute = true,
                    });
                    Logger.GetLogger<EventToast>().Info("Launched browser for {0}", _eventBaseUrl);
                }
                catch (Exception ex)
                {
                    Logger.GetLogger<EventToast>().Warn(ex, "Browser launch failed.");
                }
                FlashCopied(open, "Open");
            };
            x += open.Width + 6;
        }
        private static void FlashCopied(StandardButton btn, string originalText)
        {
            btn.Text = "âœ“ copied";
            Timer t = null;
            t = new Timer(_ =>
            {
                GameService.Overlay.QueueMainThreadUpdate(__ =>
                {
                    try { btn.Text = originalText; } catch {  }
                });
                t?.Dispose();
            }, null, TimeSpan.FromMilliseconds(1400), Timeout.InfiniteTimeSpan);
        }
    }
}
