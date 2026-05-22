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
        private readonly NotificationHistory _history;

        private readonly Panel _titleBar;
        private readonly FlowPanel _content;
        private readonly Label _statusLabel;
        private readonly StandardButton _refreshButton;
        private readonly Checkbox _notifySignupsCheckbox;
        private readonly Checkbox _notifyNewEventsCheckbox;
        private readonly Timer _autoRefresh;
        private CancellationTokenSource _inFlight;

        // Drag state · the title bar listens for mouse events and we update
        // Location on every move. Tracking the offset within the bar keeps
        // the cursor anchored to the exact pixel the user grabbed.
        private bool _dragging;
        private Point _dragOffset;

        private const int WindowWidth  = 480;
        private const int WindowHeight = 560;
        private const int TitleBarHeight = 32;
        private const int TogglesHeight  = 28;

        public MenuWindow(ApiClient api, ModuleSettings settings, NotificationService notify, NotificationHistory history)
        {
            _api = api;
            _settings = settings;
            _notify = notify;
            _history = history;

            Width = WindowWidth;
            Height = WindowHeight;
            BackgroundColor = new Color(14, 14, 18, 240);
            Visible = false;
            ZIndex = 200;

            // ── Title bar ────────────────────────────────────────────────
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

            // Drag bootstrap · only the title-bar press starts a drag, but
            // tracking the mouse needs to be GLOBAL because the cursor leaves
            // the bar immediately. We subscribe to the global Input.Mouse
            // events on press and unsubscribe on release; that way the
            // window follows the cursor anywhere on screen.
            _titleBar.LeftMouseButtonPressed += OnTitlePressed;

            // ── Toggle row ───────────────────────────────────────────────
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

            // (Test toast button removed · the live toast pipeline now
            // gives plenty of feedback via the new "Signed up" trigger
            // and the wider browse list.)

            // ── Status line ──────────────────────────────────────────────
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

            // ── Scrollable content area ──────────────────────────────────
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

        // ── Drag handlers ────────────────────────────────────────────────

        private void OnTitlePressed(object sender, MouseEventArgs e)
        {
            if (_dragging) return;
            _dragging = true;
            var mouse = GameService.Input.Mouse.Position;
            _dragOffset = new Point(mouse.X - Location.X, mouse.Y - Location.Y);

            // Track on the GLOBAL mouse, not the title bar · the cursor
            // moves outside the bar within the first pixel of drag and the
            // bar's events stop firing.
            GameService.Input.Mouse.MouseMoved += OnGlobalMouseMoved;
            GameService.Input.Mouse.LeftMouseButtonReleased += OnGlobalReleased;
        }

        private void OnGlobalMouseMoved(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var mouse = GameService.Input.Mouse.Position;
            var screen = GameService.Graphics.SpriteScreen;
            // Clamp so the title bar stays on-screen · prevents users from
            // dragging the panel out of reach.
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

        // ── Show / refresh ───────────────────────────────────────────────

        public void ShowMenu()
        {
            var screen = GameService.Graphics.SpriteScreen;
            Parent = screen;
            // Open near the corner icon · top-center, just below the bar.
            Location = new Point(
                (screen.Width - Width) / 2,
                Math.Max(40, screen.Height / 6));
            Visible = true;
            ZIndex = 1000;

            // Re-sync toggles with persisted values · the user may have
            // changed them in Blish's settings panel while menu was closed.
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

                // Officer approval queue is best-effort · users who aren't
                // in any officer role just get an empty list. Failure here
                // shouldn't poison the main menu render.
                PendingApprovalsResponse approvals = null;
                try { approvals = await _api.FetchApprovalsAsync(baseUrl, bearer, cancel).ConfigureAwait(false); } catch { }

                GameService.Overlay.QueueMainThreadUpdate(_ => Populate(resp, approvals));
            }
            catch (TaskCanceledException) { /* stacked refresh · ignore */ }
            catch (Exception ex)
            {
                Logger.GetLogger<MenuWindow>().Warn(ex, "Browse refresh failed.");
                _statusLabel.Text = "Refresh failed · try again.";
            }
            finally
            {
                _refreshButton.Enabled = true;
            }
        }

        private void Populate(UpcomingResponse resp, PendingApprovalsResponse approvals = null)
        {
            _content.ClearChildren();

            if (approvals?.Pending != null && approvals.Pending.Length > 0)
            {
                AddSectionHeader($"Pending approvals · {approvals.Pending.Length}");
                foreach (var p in approvals.Pending)
                {
                    AddApprovalRow(p);
                }
            }

            AddSectionHeader($"My signups (next 7 days)  ·  {resp.MySignups?.Length ?? 0}");
            if (resp.MySignups == null || resp.MySignups.Length == 0)
            {
                AddEmptyRow("Nothing scheduled. Sign up to events on the mobile app to see them here.");
            }
            else
            {
                foreach (var ev in resp.MySignups)
                {
                    AddEventRow(ev, showSqjoin: true);
                }
            }

            AddSectionHeader($"New guild events (last 24h)  ·  {resp.NewGuildEvents?.Length ?? 0}");
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

            AddSectionHeader($"Guild announcements (last 24h)  ·  {resp.NewAnnouncements?.Length ?? 0}");
            if (resp.NewAnnouncements == null || resp.NewAnnouncements.Length == 0)
            {
                AddEmptyRow("No recent guild announcements.");
            }
            else
            {
                foreach (var ann in resp.NewAnnouncements)
                {
                    AddAnnouncementRow(ann);
                }
            }

            // Recent toasts · so users can re-check anything they dismissed
            // or missed while alt-tabbed out. In-memory only · clears on
            // module reload.
            var history = _history?.Snapshot();
            if (history != null && history.Count > 0)
            {
                AddSectionHeader($"Recent notifications  ·  last {Math.Min(history.Count, 10)}");
                foreach (var entry in history.GetRange(0, Math.Min(history.Count, 10)))
                {
                    AddHistoryRow(entry);
                }
            }

            _statusLabel.Text = $"Updated · server time {resp.ServerTime:HH:mm:ss}";
        }

        private void AddAnnouncementRow(Announcement ann)
        {
            var titleFont = _settings.TitleFont();
            var bodyFont  = _settings.BodyFont();
            int titleH    = (int)titleFont.LineHeight;
            int bodyH     = (int)bodyFont.LineHeight;

            // Body wraps into up to 3 body-font lines · longer content
            // gets truncated visually by the label clip. The full body is
            // still in the announcement's `Content` field if we later add
            // a "view full" affordance.
            int bodyBlock = bodyH * 3 + 4;
            int padTop    = 8;
            int padMid    = 4;
            int padBot    = 8;
            int rowHeight = padTop + titleH + padMid + bodyH + padMid + bodyBlock + padBot;

            var accent = Color.Goldenrod;
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
                BackgroundColor = accent,
                Location = new Point(0, 0),
                Width = 4,
                Height = rowHeight,
            };

            var head = string.IsNullOrWhiteSpace(ann.GuildTag)
                ? $"📢 {ann.GuildName}"
                : $"📢 [{ann.GuildTag}] {ann.GuildName}";
            new Label
            {
                Parent = row,
                Text = head,
                Font = titleFont,
                TextColor = accent,
                Location = new Point(12, padTop),
                Width = row.Width - 22,
                Height = titleH + 2,
                AutoSizeWidth = false,
            };

            var senderAndTime = string.IsNullOrWhiteSpace(ann.SenderAccountName)
                ? FormatAgo(ann.CreatedAt)
                : $"{ann.SenderAccountName} · {FormatAgo(ann.CreatedAt)}";
            new Label
            {
                Parent = row,
                Text = string.IsNullOrWhiteSpace(ann.Title)
                    ? senderAndTime
                    : $"{ann.Title} · {senderAndTime}",
                Font = bodyFont,
                TextColor = new Color(220, 200, 140),
                Location = new Point(12, padTop + titleH + padMid),
                Width = row.Width - 22,
                Height = bodyH + 2,
                AutoSizeWidth = false,
            };

            new Label
            {
                Parent = row,
                Text = string.IsNullOrWhiteSpace(ann.Content) ? "(no content)" : ann.Content,
                Font = bodyFont,
                TextColor = new Color(210, 210, 210),
                Location = new Point(12, padTop + titleH + padMid + bodyH + padMid),
                Width = row.Width - 22,
                Height = bodyBlock,
                AutoSizeWidth = false,
                WrapText = true,
            };
        }

        private void AddApprovalRow(PendingApproval p)
        {
            var typeColor = EventColors.For(p.EventType, _settings.ColorTheme.Value);
            var titleFont = _settings.TitleFont();
            var bodyFont  = _settings.BodyFont();
            var row = new Panel
            {
                Parent = _content,
                Width = _content.Width - 16,
                Height = (int)titleFont.LineHeight + (int)bodyFont.LineHeight + 56,
                BackgroundColor = new Color(24, 24, 30, 220),
            };
            new Panel
            {
                Parent = row,
                BackgroundColor = typeColor,
                Location = new Point(0, 0),
                Width = 4,
                Height = row.Height,
            };
            new Label
            {
                Parent = row,
                Text = $"{p.EventTitle} · {PrettyType(p.EventType)}",
                Font = titleFont,
                TextColor = typeColor,
                Location = new Point(12, 6),
                Width = row.Width - 22,
                Height = (int)titleFont.LineHeight + 2,
                AutoSizeWidth = false,
            };

            var spec = !string.IsNullOrWhiteSpace(p.CharacterEliteSpec) ? p.CharacterEliteSpec : p.CharacterProfession;
            var applicant = !string.IsNullOrWhiteSpace(p.ApplicantAccountName)
                ? p.ApplicantAccountName
                : (p.ApplicantDisplayName ?? p.ApplicantUsername ?? "?");
            new Label
            {
                Parent = row,
                Text = $"{applicant} · {p.CharacterName} ({spec}) · {p.GuildName}",
                Font = bodyFont,
                TextColor = new Color(210, 210, 210),
                Location = new Point(12, 10 + (int)titleFont.LineHeight),
                Width = row.Width - 22,
                Height = (int)bodyFont.LineHeight + 2,
                AutoSizeWidth = false,
            };

            var btnY = row.Height - 32;
            var approve = new StandardButton
            {
                Parent = row,
                Text = "Approve",
                Width = 90,
                Height = 26,
                Location = new Point(12, btnY),
            };
            approve.Click += (_, __) => _ = DecideAsync(p.SignupId, "approved", approve);

            var reject = new StandardButton
            {
                Parent = row,
                Text = "Reject",
                Width = 80,
                Height = 26,
                Location = new Point(108, btnY),
            };
            reject.Click += (_, __) => _ = DecideAsync(p.SignupId, "rejected", reject);
        }

        private async Task DecideAsync(string signupId, string decision, StandardButton btn)
        {
            btn.Enabled = false;
            var baseUrl = _settings.ApiBaseUrl.Value;
            var bearer  = _settings.CachedBearer.Value;
            var result = await _api.DecideApprovalAsync(baseUrl, bearer, signupId, decision, CancellationToken.None).ConfigureAwait(false);

            // Older addon installs hold a bearer issued before addon:write was
            // added to the exchange · /decide replies 401/403. Run a fresh
            // exchange and retry once.
            if (!result.ok && (result.status == 401 || result.status == 403)
                && !string.IsNullOrWhiteSpace(_settings.Gw2ApiKey.Value))
            {
                _settings.CachedBearer.Value = string.Empty;
                var fresh = await _api.ExchangeAsync(baseUrl, _settings.Gw2ApiKey.Value, CancellationToken.None).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(fresh))
                {
                    _settings.CachedBearer.Value = fresh;
                    result = await _api.DecideApprovalAsync(baseUrl, fresh, signupId, decision, CancellationToken.None).ConfigureAwait(false);
                }
            }

            bool ok = result.ok;
            GameService.Overlay.QueueMainThreadUpdate(_ =>
            {
                try { btn.Text = ok ? "✓" : "failed"; } catch { }
            });
            if (ok)
            {
                await Task.Delay(700).ConfigureAwait(false);
                GameService.Overlay.QueueMainThreadUpdate(gt => { var _t = RefreshAsync(); });
            }
            else
            {
                await Task.Delay(1500).ConfigureAwait(false);
                GameService.Overlay.QueueMainThreadUpdate(_ =>
                {
                    try { btn.Enabled = true; btn.Text = decision == "approved" ? "Approve" : "Reject"; } catch { }
                });
            }
        }

        private void AddHistoryRow(HistoryEntry entry)
        {
            var typeColor = EventColors.For(entry.EventType, _settings.ColorTheme.Value);
            var bodyFont = _settings.BodyFont();
            var titleFont = _settings.TitleFont();

            var row = new Panel
            {
                Parent = _content,
                Width = _content.Width - 16,
                Height = (int)titleFont.LineHeight + (int)bodyFont.LineHeight + 16,
                BackgroundColor = new Color(18, 18, 22, 200),
            };
            new Panel
            {
                Parent = row,
                BackgroundColor = typeColor,
                Location = new Point(0, 0),
                Width = 3,
                Height = row.Height,
            };
            new Label
            {
                Parent = row,
                Text = entry.Title,
                Font = titleFont,
                TextColor = typeColor,
                Location = new Point(10, 6),
                Width = row.Width - 50,
                Height = (int)titleFont.LineHeight + 2,
                AutoSizeWidth = false,
            };
            // × dismiss · removes the entry from the in-memory ring buffer
            // and disposes the row directly. Avoids a network round-trip we
            // don't need for a purely local UI action.
            var dismiss = new StandardButton
            {
                Parent = row,
                Text = "×",
                Width = 32,
                Height = 24,
                Location = new Point(row.Width - 40, 4),
            };
            var rowToDispose = row;
            dismiss.Click += (_, __) =>
            {
                _history.Remove(entry);
                try { rowToDispose.Dispose(); } catch { }
            };
            new Label
            {
                Parent = row,
                Text = $"{entry.Subtitle} · {FormatAgo(entry.At)}",
                Font = bodyFont,
                TextColor = new Color(180, 180, 180),
                Location = new Point(10, 8 + (int)titleFont.LineHeight),
                Width = row.Width - 20,
                Height = (int)bodyFont.LineHeight + 2,
                AutoSizeWidth = false,
            };
        }

        private static string FormatAgo(DateTime past)
        {
            var seconds = (DateTime.UtcNow - past).TotalSeconds;
            if (seconds < 60) return $"{(int)seconds}s ago";
            if (seconds < 3600) return $"{(int)(seconds / 60)}m ago";
            if (seconds < 86400) return $"{(int)(seconds / 3600)}h ago";
            return $"{(int)(seconds / 86400)}d ago";
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
            var typeColor = EventColors.For(ev.Type, _settings.ColorTheme.Value);
            var titleFont = _settings.TitleFont();
            var bodyFont  = _settings.BodyFont();

            int titleH = (int)titleFont.LineHeight;
            int bodyH  = (int)bodyFont.LineHeight;
            int padTop = 8;
            int padMid = 6;
            int btnH   = 26;
            int padBot = 8;

            // Subtitle line2 carries character/KP/boss info · for fractals
            // the boss list can be long, so we let it wrap onto a second
            // body-font line and grow the row accordingly. line1 stays on
            // a single line · it's short by construction.
            var subtitleLine1 = BuildSubtitleLine1(ev);
            var subtitleLine2 = BuildSubtitleLine2(ev);
            bool hasLine2 = !string.IsNullOrWhiteSpace(subtitleLine2);
            int line2WrapLines = hasLine2 ? EstimateWrapLines(subtitleLine2, _content.Width - 38, 7) : 0;
            if (line2WrapLines < 1 && hasLine2) line2WrapLines = 1;
            if (line2WrapLines > 2) line2WrapLines = 2;

            int titleY     = padTop;
            int subtitleY  = titleY + titleH + 2;
            int subtitle2Y = subtitleY + bodyH + 2;
            int subtitle2H = bodyH * line2WrapLines + (line2WrapLines > 1 ? 2 : 0);
            int buttonY    = (hasLine2 ? subtitle2Y + subtitle2H : subtitleY + bodyH) + padMid;
            int rowHeight  = buttonY + btnH + padBot;

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
                Text = (ev.IsRecurring ? "[R] " : string.Empty) + (string.IsNullOrWhiteSpace(ev.Title) ? PrettyType(ev.Type) : ev.Title),
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
                Text = subtitleLine1,
                Font = bodyFont,
                TextColor = new Color(210, 210, 210),
                Location = new Point(12, subtitleY),
                Width = row.Width - 22,
                Height = bodyH + 2,
                AutoSizeWidth = false,
            };
            if (hasLine2)
            {
                new Label
                {
                    Parent = row,
                    Text = subtitleLine2,
                    Font = bodyFont,
                    TextColor = new Color(180, 180, 180),
                    Location = new Point(12, subtitle2Y),
                    Width = row.Width - 22,
                    Height = subtitle2H,
                    AutoSizeWidth = false,
                    WrapText = true,
                };
            }

            const int BtnW = 80;
            var x = 12;
            var commander = ev.CommanderAccountName;
            if (!string.IsNullOrWhiteSpace(commander))
            {
                if (showSqjoin)
                {
                    AddCopyButton(row, "/sqjoin", $"/sqjoin {commander}", ref x, buttonY, BtnW);
                }
                AddWhisperButton(row, commander, ref x, buttonY, BtnW);
            }
            if (!string.IsNullOrWhiteSpace(ev.VoiceChannelUrl))
            {
                AddVoiceButton(row, ev.VoiceChannelUrl, ref x, buttonY, BtnW);
            }
            // Check-in only when the event hasn't started yet · clicking
            // "check in" after the squad has pulled would be pointless and
            // would only confuse the user.
            if (ev is MySignup signup
                && signup.CheckinStatus == "pending"
                && signup.ScheduledAt > DateTime.UtcNow)
            {
                AddCheckinButton(row, signup.Id, ref x, buttonY, BtnW);
            }

            var openUrl = $"https://tyriaplanner.com/event/{ev.Id}";
            var open = new StandardButton
            {
                Parent = row,
                Text = "Open",
                Width = BtnW,
                Height = 24,
                Location = new Point(x, buttonY),
            };
            open.Click += (_, __) =>
            {
                if (SafeUrl.IsAllowed(openUrl)) Clipboard.Set(openUrl);
                SafeUrl.Open(openUrl);
                FlashCopied(open, "Open");
            };
        }

        private void AddCheckinButton(Container parent, string eventId, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = parent,
                Text = "Check in",
                Width = width,
                Height = 24,
                Location = new Point(x, y),
            };
            btn.Click += async (_, __) =>
            {
                btn.Enabled = false;
                btn.Text = "...";
                var baseUrl = _settings.ApiBaseUrl.Value;
                var bearer  = _settings.CachedBearer.Value;
                bool ok = await _api.CheckinAsync(baseUrl, bearer, eventId, CancellationToken.None).ConfigureAwait(false);
                GameService.Overlay.QueueMainThreadUpdate(_2 =>
                {
                    try
                    {
                        btn.Text = ok ? "✓ checked in" : "failed";
                        if (!ok)
                        {
                            Timer t = null;
                            t = new Timer(__2 =>
                            {
                                GameService.Overlay.QueueMainThreadUpdate(___ =>
                                {
                                    try { btn.Enabled = true; btn.Text = "Check in"; } catch { }
                                });
                                t?.Dispose();
                            }, null, TimeSpan.FromSeconds(1.5), Timeout.InfiniteTimeSpan);
                        }
                    }
                    catch { }
                });
            };
            x += btn.Width + 6;
        }

        private static void AddVoiceButton(Container parent, string url, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = parent,
                Text = "Voice",
                Width = width,
                Height = 24,
                Location = new Point(x, y),
            };
            btn.Click += (_, __) =>
            {
                if (SafeUrl.IsAllowed(url)) Clipboard.Set(url);
                SafeUrl.Open(url);
                FlashCopied(btn, "Voice");
            };
            x += btn.Width + 6;
        }

        // Plain clipboard copy of the bare account name. User opens chat
        // themselves and types "/w " then pastes + their message. Every
        // keyboard-simulation route (SendInput, Intern.Keyboard,
        // GameIntegration.Chat.Paste) either lost focus, dropped the
        // modifier, or crashed the host · so clipboard-only it is.
        private static void AddWhisperButton(Container parent, string accountName, ref int x, int y, int width)
        {
            var btn = new StandardButton
            {
                Parent = parent,
                Text = "Copy name",
                Width = width,
                Height = 26,
                Location = new Point(x, y),
            };
            btn.Click += (_, __) =>
            {
                try { Clipboard.Set(accountName); } catch { }
                FlashCopied(btn, "Copy name");
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
            btn.Text = "✓ copied";
            Timer t = null;
            t = new Timer(_ =>
            {
                GameService.Overlay.QueueMainThreadUpdate(__ =>
                {
                    try { btn.Text = original; } catch { /* button gone · row dropped */ }
                });
                t?.Dispose();
            }, null, TimeSpan.FromMilliseconds(1400), Timeout.InfiniteTimeSpan);
        }

        // First line · short, always present · guild + commander + type +
        // signup count (the headline metric users wanted on the card).
        private static string BuildSubtitleLine1(EventBase ev)
        {
            var guild = string.IsNullOrWhiteSpace(ev.GuildName) ? "Public" : ev.GuildName;
            var commander = !string.IsNullOrWhiteSpace(ev.CommanderAccountName)
                ? ev.CommanderAccountName
                : ev.CommanderDisplayName ?? "?";
            var head = $"{guild} · {commander} · {PrettyType(ev.Type)}";
            if (ev.MaxSignups > 0)
            {
                head += $" · {ev.SignupCount}/{ev.MaxSignups} signed up";
            }
            return head;
        }

        // Second line · longer details · character + KP + boss preview.
        // Empty string returned when none of these apply, so the caller can
        // skip rendering the label.
        private static string BuildSubtitleLine2(EventBase ev)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (ev is MySignup ms && ms.SignupCharacter != null)
            {
                var spec = !string.IsNullOrWhiteSpace(ms.SignupCharacter.EliteSpec)
                    ? ms.SignupCharacter.EliteSpec
                    : ms.SignupCharacter.Profession;
                parts.Add($"as {ms.SignupCharacter.Name} ({spec})");
            }

            if (ev.KpRequirement != null && ev.KpRequirement.Amount > 0)
            {
                var modeShort = ev.KpRequirement.Mode == "average" ? "avg" : "min";
                parts.Add($"KP {modeShort} {ev.KpRequirement.Amount}");
            }

            if (ev.BossSlugs != null && ev.BossSlugs.Length > 0)
            {
                // Show all bosses · users specifically asked us to stop
                // truncating with "..." when a fractal day had more than four.
                parts.Add(string.Join(", ", ev.BossSlugs));
            }

            return string.Join(" · ", parts);
        }

        private static string BuildCountdown(EventBase ev)
        {
            var totalMinutes = (ev.ScheduledAt - DateTime.UtcNow).TotalMinutes;
            if (totalMinutes < 0) return "started";
            if (totalMinutes < 1) return "now";
            if (totalMinutes < 60) return $"in {(int)Math.Round(totalMinutes)}m";
            if (totalMinutes < 24 * 60)
            {
                int h = (int)(totalMinutes / 60);
                int m = (int)Math.Round(totalMinutes - h * 60);
                if (m >= 60) { h += 1; m = 0; }
                return m > 0 ? $"in {h}h {m}m" : $"in {h}h";
            }
            int d = (int)(totalMinutes / (24 * 60));
            int rh = (int)Math.Round((totalMinutes - d * 24 * 60) / 60);
            if (rh >= 24) { d += 1; rh = 0; }
            return rh > 0 ? $"in {d}d {rh}h" : $"in {d}d";
        }

        // Rough wrap estimate · "how many lines will this text take when
        // rendered into `widthPx` at roughly `pxPerChar` pixels per character?"
        // Blish doesn't expose font measurement here, so we use a coarse
        // heuristic that is good enough to decide between a 1- and 2-line
        // row. We never grow past 2 lines.
        private static int EstimateWrapLines(string text, int widthPx, int pxPerChar)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int charsPerLine = Math.Max(1, widthPx / pxPerChar);
            int lines = (int)Math.Ceiling((double)text.Length / charsPerLine);
            return Math.Max(1, lines);
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
