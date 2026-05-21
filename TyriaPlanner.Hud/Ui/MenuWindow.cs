using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TyriaPlanner.Hud.Api;
using TyriaPlanner.Hud.Services;
using TyriaPlanner.Hud.Settings;
namespace TyriaPlanner.Hud.Ui
{
    public sealed class MenuWindow : Container
    {
        private readonly ApiClient _api;
        private readonly ModuleSettings _settings;
        private readonly NotificationService _notify;
        private readonly Panel _titleBar;
        private readonly FlowPanel _content;
        private readonly Label _statusLabel;
        private readonly StandardButton _refreshButton;
        private readonly Checkbox _notifySignupsCheckbox;
        private readonly Checkbox _notifyNewEventsCheckbox;
        private readonly Timer _autoRefresh;
        private CancellationTokenSource _inFlight;
        private bool _dragging;
        private Point _dragOffset;
        private const int WindowWidth  = 480;
        private const int WindowHeight = 560;
        private const int TitleBarHeight = 32;
        private const int TogglesHeight  = 28;
        public MenuWindow(ApiClient api, ModuleSettings settings, NotificationService notify)
        {
            _api = api;
            _settings = settings;
            _notify = notify;
            Width = WindowWidth;
            Height = WindowHeight;
            BackgroundColor = new Color(14, 14, 18, 240);
            Visible = false;
            ZIndex = 200;
            _titleBar = new Panel
            {
                Parent = this,
                Width = WindowWidth,
                Height = TitleBarHeight,
                Location = Point.Zero,
                BackgroundColor = new Color(28, 26, 20, 255),
            };
            new Label
            {
                Parent = _titleBar,
                Text = "Tyria Planner",
                Font = GameService.Content.DefaultFont16,
                TextColor = Color.Goldenrod,
                Location = new Point(12, 6),
                Width = 220,
                Height = 22,
                AutoSizeWidth = false,
            };
            var close = new StandardButton
            {
                Parent = _titleBar,
                Text = "X",
                Width = 28,
                Height = 22,
                Location = new Point(WindowWidth - 36, 5),
            };
            close.Click += (_, __) => HideMenu();
            _refreshButton = new StandardButton
            {
                Parent = _titleBar,
                Text = "Refresh",
                Width = 72,
                Height = 22,
                Location = new Point(WindowWidth - 36 - 78, 5),
            };
            _refreshButton.Click += (_, __) => _ = RefreshAsync();
            _titleBar.LeftMouseButtonPressed += OnTitlePressed;
            var togglesRow = new Panel
            {
                Parent = this,
                Width = WindowWidth,
                Height = TogglesHeight,
                Location = new Point(0, TitleBarHeight),
                BackgroundColor = new Color(22, 22, 26, 255),
            };
            _notifySignupsCheckbox = new Checkbox
            {
                Parent = togglesRow,
                Text = "Signup reminders",
                Location = new Point(12, 6),
                Checked = _settings.NotifyOwnSignups.Value,
            };
            _notifySignupsCheckbox.CheckedChanged += (_, args) =>
                _settings.NotifyOwnSignups.Value = args.Checked;
            _notifyNewEventsCheckbox = new Checkbox
            {
                Parent = togglesRow,
                Text = "New guild events",
                Location = new Point(170, 6),
                Checked = _settings.NotifyNewGuildEvents.Value,
            };
            _notifyNewEventsCheckbox.CheckedChanged += (_, args) =>
                _settings.NotifyNewGuildEvents.Value = args.Checked;
            _statusLabel = new Label
            {
                Parent = this,
                Text = "Loading...",
                Font = GameService.Content.DefaultFont12,
                TextColor = new Color(190, 190, 190),
                Location = new Point(12, TitleBarHeight + TogglesHeight + 6),
                Width = WindowWidth - 24,
                Height = 18,
                AutoSizeWidth = false,
            };
            var contentTop = TitleBarHeight + TogglesHeight + 28;
            _content = new FlowPanel
            {
                Parent = this,
                Location = new Point(8, contentTop),
                Width = WindowWidth - 16,
                Height = WindowHeight - contentTop - 8,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ControlPadding = new Vector2(0, 6),
                OuterControlPadding = new Vector2(0, 0),
            };
            _autoRefresh = new Timer(_ =>
            {
                if (!Visible) return;
                GameService.Overlay.QueueMainThreadUpdate(__ => { _ = RefreshAsync(); });
            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        private void OnTitlePressed(object sender, MouseEventArgs e)
        {
            if (_dragging) return;
            _dragging = true;
            var mouse = GameService.Input.Mouse.Position;
            _dragOffset = new Point(mouse.X - Location.X, mouse.Y - Location.Y);
            GameService.Input.Mouse.MouseMoved += OnGlobalMouseMoved;
            GameService.Input.Mouse.LeftMouseButtonReleased += OnGlobalReleased;
        }
        private void OnGlobalMouseMoved(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var mouse = GameService.Input.Mouse.Position;
            var screen = GameService.Graphics.SpriteScreen;
            var newX = Math.Max(-WindowWidth + 80, Math.Min(screen.Width - 80, mouse.X - _dragOffset.X));
            var newY = Math.Max(0, Math.Min(screen.Height - 40, mouse.Y - _dragOffset.Y));
            Location = new Point(newX, newY);
        }
        private void OnGlobalReleased(object sender, MouseEventArgs e)
        {
            _dragging = false;
            GameService.Input.Mouse.MouseMoved -= OnGlobalMouseMoved;
            GameService.Input.Mouse.LeftMouseButtonReleased -= OnGlobalReleased;
        }
        public void ShowMenu()
        {
            var screen = GameService.Graphics.SpriteScreen;
            Parent = screen;
            Location = new Point(
                (screen.Width - Width) / 2,
                Math.Max(40, screen.Height / 6));
            Visible = true;
            ZIndex = 1000;
            _notifySignupsCheckbox.Checked = _settings.NotifyOwnSignups.Value;
            _notifyNewEventsCheckbox.Checked = _settings.NotifyNewGuildEvents.Value;
            _autoRefresh.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
            _ = RefreshAsync();
        }
        public void HideMenu()
        {
            Visible = false;
            _autoRefresh.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        public void Toggle()
        {
            if (Visible) HideMenu();
            else ShowMenu();
        }
        private async Task RefreshAsync()
        {
            _inFlight?.Cancel();
            _inFlight = new CancellationTokenSource();
            var cancel = _inFlight.Token;
            _statusLabel.Text = "Loading...";
            _refreshButton.Enabled = false;
            try
            {
                var baseUrl = _settings.ApiBaseUrl.Value;
                var bearer  = _settings.CachedBearer.Value;
                if (string.IsNullOrWhiteSpace(bearer))
                {
                    if (string.IsNullOrWhiteSpace(_settings.Gw2ApiKey.Value))
                    {
                        _statusLabel.Text = "Paste your GW2 API key in module settings first.";
                        return;
                    }
                    bearer = await _api.ExchangeAsync(baseUrl, _settings.Gw2ApiKey.Value, cancel).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(bearer))
                    {
                        _statusLabel.Text = "GW2 key not recognised by Tyria Planner.";
                        return;
                    }
                    _settings.CachedBearer.Value = bearer;
                }
                var resp = await _api.FetchBrowseAsync(baseUrl, bearer, cancel).ConfigureAwait(false);
                if (resp == null)
                {
                    _settings.CachedBearer.Value = string.Empty;
                    bearer = await _api.ExchangeAsync(baseUrl, _settings.Gw2ApiKey.Value, cancel).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(bearer))
                    {
                        _settings.CachedBearer.Value = bearer;
                        resp = await _api.FetchBrowseAsync(baseUrl, bearer, cancel).ConfigureAwait(false);
                    }
                }
                if (resp == null)
                {
                    _statusLabel.Text = "Failed to load. Check your API key or network.";
                    return;
                }
                GameService.Overlay.QueueMainThreadUpdate(_ => Populate(resp));
            }
            catch (TaskCanceledException) {  }
            catch (Exception ex)
            {
                Logger.GetLogger<MenuWindow>().Warn(ex, "Browse refresh failed.");
                _statusLabel.Text = "Refresh failed Â· try again.";
            }
            finally
            {
                _refreshButton.Enabled = true;
            }
        }
        private void Populate(UpcomingResponse resp)
        {
            _content.ClearChildren();
            AddSectionHeader($"My signups (next 7 days)  Â·  {resp.MySignups?.Length ?? 0}");
            if (resp.MySignups == null || resp.MySignups.Length == 0)
            {
                AddEmptyRow("Nothing scheduled. Sign up to events on the website to see them here.");
            }
            else
            {
                foreach (var ev in resp.MySignups)
                {
                    AddEventRow(ev, showSqjoin: true);
                }
            }
            AddSectionHeader($"New guild events (last 24h)  Â·  {resp.NewGuildEvents?.Length ?? 0}");
            if (resp.NewGuildEvents == null || resp.NewGuildEvents.Length == 0)
            {
                AddEmptyRow("No new guild events you haven't signed up to yet.");
            }
            else
            {
                foreach (var ev in resp.NewGuildEvents)
                {
                    AddEventRow(ev, showSqjoin: false);
                }
            }
            _statusLabel.Text = $"Updated Â· server time {resp.ServerTime:HH:mm:ss}";
        }
        private void AddSectionHeader(string text)
        {
            var font = _settings.TitleFont();
            new Label
            {
                Parent = _content,
                Text = text,
                Font = font,
                TextColor = Color.Goldenrod,
                Width = _content.Width - 8,
                Height = (int)font.LineHeight + 6,
                AutoSizeWidth = false,
            };
        }
        private void AddEmptyRow(string text)
        {
            var font = _settings.BodyFont();
            new Label
            {
                Parent = _content,
                Text = text,
                Font = font,
                TextColor = new Color(160, 160, 160),
                Width = _content.Width - 16,
                Height = (int)font.LineHeight * 2 + 6,
                AutoSizeWidth = false,
                WrapText = true,
            };
        }
        private void AddEventRow(EventBase ev, bool showSqjoin)
        {
            var typeColor = EventColors.For(ev.Type);
            var titleFont = _settings.TitleFont();
            var bodyFont  = _settings.BodyFont();
            int titleH = (int)titleFont.LineHeight;
            int bodyH  = (int)bodyFont.LineHeight;
            int padTop = 8;
            int padMid = 6;     
            int btnH   = 26;
            int padBot = 8;
            int titleY    = padTop;
            int subtitleY = titleY + titleH + 2;
            int buttonY   = subtitleY + bodyH + padMid;
            int rowHeight = buttonY + btnH + padBot;
            var row = new Panel
            {
                Parent = _content,
                Width = _content.Width - 16,
                Height = rowHeight,
                BackgroundColor = new Color(22, 22, 26, 220),
            };
            new Panel
            {
                Parent = row,
                BackgroundColor = typeColor,
                Location = new Point(0, 0),
                Width = 4,
                Height = rowHeight,
            };
            int countdownWidth = 100;
            new Label
            {
                Parent = row,
                Text = BuildCountdown(ev),
                Font = titleFont,
                TextColor = typeColor,
                Location = new Point(row.Width - countdownWidth - 12, titleY),
                Width = countdownWidth,
                Height = titleH + 2,
                AutoSizeWidth = false,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            new Label
            {
                Parent = row,
                Text = string.IsNullOrWhiteSpace(ev.Title) ? PrettyType(ev.Type) : ev.Title,
                Font = titleFont,
                TextColor = typeColor,
                Location = new Point(12, titleY),
                Width = row.Width - countdownWidth - 30,
                Height = titleH + 2,
                AutoSizeWidth = false,
            };
            new Label
            {
                Parent = row,
                Text = BuildSubtitle(ev),
                Font = bodyFont,
                TextColor = new Color(210, 210, 210),
                Location = new Point(12, subtitleY),
                Width = row.Width - 22,
                Height = bodyH + 2,
                AutoSizeWidth = false,
            };
            var x = 12;
            var commander = ev.CommanderAccountName;
            if (!string.IsNullOrWhiteSpace(commander))
            {
                if (showSqjoin)
                {
                    AddCopyButton(row, "/sqjoin", $"/sqjoin {commander}", ref x, buttonY, 84);
                }
                AddWhisperButton(row, commander, ref x, buttonY, 84);
            }
            var openUrl = $"https://tyriaplanner.com/event/{ev.Id}";
            var open = new StandardButton
            {
                Parent = row,
                Text = "Open",
                Width = 70,
                Height = 24,
                Location = new Point(x, buttonY),
            };
            open.Click += (_, __) =>
            {
                Clipboard.Set(openUrl);
                try { Process.Start(new ProcessStartInfo { FileName = openUrl, UseShellExecute = true }); }
                catch (Exception ex) { Logger.GetLogger<MenuWindow>().Warn(ex, "Browser open failed."); }
                FlashCopied(open, "Open");
            };
        }
        private static void AddWhisperButton(Container parent, string accountName, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = parent,
                Text = "/whisper",
                Width = width,
                Height = 26,
                Location = new Point(x, y),
            };
            btn.Click += (_, __) =>
            {
                _ = WhisperOpener.OpenAsync(accountName);
                FlashCopied(btn, "/whisper");
            };
            x += width + 6;
        }
        private static void AddCopyButton(Container parent, string label, string payload, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = parent,
                Text = label,
                Width = width,
                Height = 24,
                Location = new Point(x, y),
            };
            btn.Click += (_, __) =>
            {
                Clipboard.Set(payload);
                FlashCopied(btn, label);
            };
            x += width + 6;
        }
        private static void FlashCopied(StandardButton btn, string original)
        {
            btn.Text = "âœ“ copied";
            Timer t = null;
            t = new Timer(_ =>
            {
                GameService.Overlay.QueueMainThreadUpdate(__ =>
                {
                    try { btn.Text = original; } catch {  }
                });
                t?.Dispose();
            }, null, TimeSpan.FromMilliseconds(1400), Timeout.InfiniteTimeSpan);
        }
        private static string BuildSubtitle(EventBase ev)
        {
            var guild = string.IsNullOrWhiteSpace(ev.GuildName) ? "Public" : ev.GuildName;
            var commander = !string.IsNullOrWhiteSpace(ev.CommanderAccountName)
                ? ev.CommanderAccountName
                : ev.CommanderDisplayName ?? "?";
            return $"{guild} Â· {commander} Â· {PrettyType(ev.Type)}";
        }
        private static string BuildCountdown(EventBase ev)
        {
            var minutes = (ev.ScheduledAt - DateTime.UtcNow).TotalMinutes;
            if (minutes < 0) return "started";
            if (minutes < 1) return "now";
            if (minutes < 60) return $"in {(int)Math.Round(minutes)} m";
            var hours = minutes / 60.0;
            if (hours < 24) return $"in {hours:0.#} h";
            return $"in {(int)(hours / 24)} d";
        }
        private static string PrettyType(string type)
        {
            switch (type)
            {
                case "raid":       return "Raid";
                case "strike":     return "Strike";
                case "fractal":    return "Fractal";
                case "wvw":        return "WvW";
                case "open_world": return "Open World";
                default:           return type ?? "Event";
            }
        }
        protected override void DisposeControl()
        {
            _autoRefresh?.Dispose();
            _inFlight?.Cancel();
            _inFlight?.Dispose();
            base.DisposeControl();
        }
    }
}
