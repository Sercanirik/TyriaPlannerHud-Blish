using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using TyriaPlanner.Hud.Api;
using TyriaPlanner.Hud.Settings;
namespace TyriaPlanner.Hud.Services
{
    public sealed class PollingService : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<PollingService>();
        private readonly ApiClient _api;
        private readonly ModuleSettings _settings;
        private readonly NotificationService _notify;
        private CancellationTokenSource _cancel;
        private Task _loop;
        private readonly HashSet<string> _shownReminders = new HashSet<string>();
        private readonly HashSet<string> _shownGuildEvents = new HashSet<string>();
        private readonly HashSet<string> _seenSignups = new HashSet<string>();
        private bool _seenSignupsSeeded;
        private DateTimeOffset? _lastSeen;
        public PollingService(ApiClient api, ModuleSettings settings, NotificationService notify)
        {
            _api = api;
            _settings = settings;
            _notify = notify;
        }
        public void Start()
        {
            Stop();
            _cancel = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_cancel.Token));
        }
        public void Stop()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
                _cancel.Dispose();
                _cancel = null;
            }
            _loop = null;
        }
        private async Task RunAsync(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    await PollOnceAsync(cancel).ConfigureAwait(false);
                }
                catch (TaskCanceledException) {  }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Tyria Planner poll failed; will retry next cycle.");
                }
                var delaySeconds = Math.Max(30, _settings.PollIntervalSeconds.Value);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancel).ConfigureAwait(false);
                }
                catch (TaskCanceledException) { return; }
            }
        }
        private async Task PollOnceAsync(CancellationToken cancel)
        {
            var baseUrl = _settings.ApiBaseUrl.Value;
            var gw2Key  = _settings.Gw2ApiKey.Value;
            if (string.IsNullOrWhiteSpace(gw2Key) || string.IsNullOrWhiteSpace(baseUrl))
            {
                return;
            }
            var bearer = _settings.CachedBearer.Value;
            if (string.IsNullOrWhiteSpace(bearer))
            {
                bearer = await _api.ExchangeAsync(baseUrl, gw2Key, cancel).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(bearer)) return;
                _settings.CachedBearer.Value = bearer;
            }
            var resp = await _api.FetchUpcomingAsync(baseUrl, bearer, _lastSeen, cancel).ConfigureAwait(false);
            if (resp == null)
            {
                _settings.CachedBearer.Value = string.Empty;
                var fresh = await _api.ExchangeAsync(baseUrl, gw2Key, cancel).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(fresh)) return;
                _settings.CachedBearer.Value = fresh;
                resp = await _api.FetchUpcomingAsync(baseUrl, fresh, _lastSeen, cancel).ConfigureAwait(false);
                if (resp == null) return;
            }
            _lastSeen = resp.ServerTime;
            if (_settings.NotifyOwnSignups.Value && resp.MySignups != null)
            {
                if (!_seenSignupsSeeded)
                {
                    foreach (var s in resp.MySignups) _seenSignups.Add(s.Id);
                    _seenSignupsSeeded = true;
                }
                else
                {
                    foreach (var signup in resp.MySignups)
                    {
                        if (_seenSignups.Add(signup.Id))
                        {
                            _notify.PostSignedUpToast(signup);
                        }
                    }
                }
                foreach (var signup in resp.MySignups)
                {
                    HandleSignupTriggers(signup);
                }
            }
            if (_settings.NotifyNewGuildEvents.Value && resp.NewGuildEvents != null)
            {
                foreach (var ev in resp.NewGuildEvents)
                {
                    if (_shownGuildEvents.Add(ev.Id))
                    {
                        _notify.PostNewGuildEventToast(ev);
                    }
                }
            }
        }
        private void HandleSignupTriggers(MySignup signup)
        {
            var minutesUntil = (signup.ScheduledAt - DateTime.UtcNow).TotalMinutes;
            if (minutesUntil <= 1 && minutesUntil > -2)
            {
                var key = signup.Id + "|starting";
                if (_shownReminders.Add(key))
                {
                    _notify.PostSignupReminder(signup, 0);
                    return;   
                }
            }
            var checkinAt = signup.CheckinReminderMinutes ?? 0;
            if (checkinAt > 0 && checkinAt != 5 && checkinAt != 15 && checkinAt != 30
                && minutesUntil <= checkinAt && minutesUntil > 0)
            {
                var key = signup.Id + "|checkin";
                if (_shownReminders.Add(key))
                {
                    _notify.PostCheckinOpenToast(signup);
                }
            }
            if (minutesUntil < 0) return;
            int? toFire = null;
            foreach (var t in new[] { 5, 15, 30 })
            {
                if (minutesUntil > t) continue;
                var key = signup.Id + "|t" + t;
                if (_shownReminders.Contains(key)) continue;
                toFire = t;
                break;
            }
            if (toFire == null) return;
            _shownReminders.Add(signup.Id + "|t" + toFire);
            _notify.PostSignupReminder(signup, toFire.Value);
            Logger.Info("Reminder fired Â· event={0} minutesUntil={1:0.#} threshold={2}",
                signup.Title, minutesUntil, toFire);
        }
        public void Dispose()
        {
            Stop();
        }
    }
}
