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
        private readonly string _voiceChannelUrl;
        private readonly string _eventBaseUrl;
        private readonly bool _showSqjoin;
        private readonly bool _showJoinFromAppHint;
        private readonly Action<int> _onSnooze;

        public EventToast(
            ModuleSettings settings,
            string title,
            string subtitle,
            ToastAccent accent,
            string eventType,
            string commanderAccountName,
            string eventId,
            string eventBaseUrl,
            string voiceChannelUrl = null,
            bool showSqjoin = true,
            bool showJoinFromAppHint = false,
            bool isRecurring = false,
            Action<int> onSnooze = null)
        {
            _commanderAccountName = commanderAccountName;
            _voiceChannelUrl = voiceChannelUrl;
            _eventBaseUrl = eventBaseUrl;
            _showSqjoin = showSqjoin;
            _showJoinFromAppHint = showJoinFromAppHint;
            _onSnooze = onSnooze;

            var titleFont = settings.TitleFont();
            var bodyFont  = settings.BodyFont();

            // Two button rows when snooze is offered + room for the recurring
            // badge in the title area. Heights bump accordingly.
            bool snoozeRow = _onSnooze != null;
            var baseHeight = showJoinFromAppHint ? 130 : 110;
            if (snoozeRow) baseHeight += 32;
            Height = baseHeight;
            BackgroundColor = new Color(14, 14, 18, 235);

            var typeColor = EventColors.For(eventType, settings.ColorTheme.Value);
            new Panel
            {
                Parent = this,
                BackgroundColor = typeColor,
                Location = new Point(0, 0),
                Width = 4,
                Height = baseHeight,
            };

            // Title row. Recurring events get a small "[R]" prefix · keeps
            // the title readable but flags repeating slots at a glance.
            var titleText = (isRecurring ? "[R] " : string.Empty) + title;
            new Label
            {
                Parent = this,
                Text = titleText,
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
                Height = 22,
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
                    Text = "Sign up via the mobile app first",
                    Font = bodyFont,
                    TextColor = new Color(255, 200, 90),
                    Location = new Point(12, 14 + (int)titleFont.LineHeight + (int)bodyFont.LineHeight),
                    Width = 360,
                    Height = (int)bodyFont.LineHeight + 4,
                    AutoSizeWidth = false,
                };
            }

            // Action row · /sqjoin (when signed up + commander known),
            // /whisper, Voice (if URL), Open. Snooze row sits below if present.
            var actionsY = baseHeight - (snoozeRow ? 60 : 32);
            var x = 12;

            if (_showSqjoin && !string.IsNullOrWhiteSpace(_commanderAccountName))
            {
                AddCopyButton("/sqjoin", $"/sqjoin {_commanderAccountName}", ref x, actionsY, 84);
            }
            if (!string.IsNullOrWhiteSpace(_commanderAccountName))
            {
                AddWhisperButton(_commanderAccountName, ref x, actionsY, 84);
            }
            if (!string.IsNullOrWhiteSpace(_voiceChannelUrl))
            {
                AddVoiceButton(_voiceChannelUrl, ref x, actionsY, 70);
            }
            AddOpenButton(ref x, actionsY);

            if (snoozeRow)
            {
                var sy = actionsY + 32;
                var sx = 12;
                AddSnoozeButton("Snooze 5 min",  5,  ref sx, sy);
                AddSnoozeButton("Snooze 15 min", 15, ref sx, sy);
            }
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
                Text = "Copy name",
                Location = new Point(x, y),
                Width = width,
                Height = 26,
            };
            btn.Click += (_, __) =>
            {
                // Plain clipboard copy of the bare account name. No chat
                // automation, no async, no Blish input pipeline · the user
                // opens chat themselves and types "/w " + paste + message.
                // This is the only reliable path: every keyboard-simulation
                // attempt either lost focus, got the modifier swallowed by
                // the overlay, or crashed the host.
                try { Clipboard.Set(accountName); } catch { }
                FlashCopied(btn, "Copy name");
            };
            x += width + 6;
        }

        private void AddVoiceButton(string url, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = this,
                Text = "Voice",
                Location = new Point(x, y),
                Width = width,
                Height = 26,
            };
            btn.Click += (_, __) =>
            {
                if (SafeUrl.IsAllowed(url)) Clipboard.Set(url);
                SafeUrl.Open(url);
                FlashCopied(btn, "Voice");
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
                Width = 70,
                Height = 26,
            };
            open.Click += (_, __) =>
            {
                if (SafeUrl.IsAllowed(_eventBaseUrl)) Clipboard.Set(_eventBaseUrl);
                SafeUrl.Open(_eventBaseUrl);
                FlashCopied(open, "Open");
            };
            x += open.Width + 6;
        }

        private void AddSnoozeButton(string label, int minutes, ref int x, int y)
        {
            var btn = new StandardButton
            {
                Parent = this,
                Text = label,
                Location = new Point(x, y),
                Width = 110,
                Height = 26,
            };
            btn.Click += (_, __) =>
            {
                _onSnooze?.Invoke(minutes);
                Dispose();
            };
            x += btn.Width + 6;
        }

        private static void FlashCopied(StandardButton btn, string originalText)
        {
            btn.Text = "✓ copied";
            Timer t = null;
            t = new Timer(_ =>
            {
                GameService.Overlay.QueueMainThreadUpdate(__ =>
                {
                    try { btn.Text = originalText; } catch { }
                });
                t?.Dispose();
            }, null, TimeSpan.FromMilliseconds(1400), Timeout.InfiniteTimeSpan);
        }
    }
}
