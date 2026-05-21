using System;
using System.Threading;
using Blish_HUD;
using TyriaPlanner.Hud.Settings;
using TyriaPlanner.Hud.Ui;
namespace TyriaPlanner.Hud.Services
{
    public sealed class WeeklyResetReminder : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<WeeklyResetReminder>();
        private readonly ModuleSettings _settings;
        private readonly ToastStack _stack;
        private Timer _timer;
        public WeeklyResetReminder(ModuleSettings settings, ToastStack stack)
        {
            _settings = settings;
            _stack = stack;
        }
        public void Start()
        {
            ScheduleNext();
        }
        private void ScheduleNext()
        {
            var delay = TimeUntilNextResetUtc(DateTime.UtcNow);
            _timer?.Dispose();
            _timer = new Timer(_ => OnFire(), null, delay, Timeout.InfiniteTimeSpan);
            Logger.Info("Weekly reset reminder scheduled in {0:0.#} h", delay.TotalHours);
        }
        private void OnFire()
        {
            if (_settings.NotifyWeeklyReset.Value)
            {
                _stack.Push(new EventToast(
                    settings: _settings,
                    title: "Weekly raid reset",
                    subtitle: "Refresh your kill-proofs Â· new week is live",
                    accent: ToastAccent.NewEvent,
                    eventType: "raid",
                    commanderAccountName: null,
                    eventId: "weekly-reset-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    eventBaseUrl: "https://tyriaplanner.com",
                    showSqjoin: false,
                    showJoinFromAppHint: false));
            }
            ScheduleNext();
        }
        public static TimeSpan TimeUntilNextResetUtc(DateTime nowUtc)
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)nowUtc.DayOfWeek + 7) % 7;
            var candidate = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 7, 30, 0, DateTimeKind.Utc)
                .AddDays(daysUntilMonday);
            if (candidate <= nowUtc) candidate = candidate.AddDays(7);
            return candidate - nowUtc;
        }
        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
