using System;
using System.Diagnostics;
using System.Media;
using Blish_HUD;
using TyriaPlanner.Hud.Api;
using TyriaPlanner.Hud.Settings;
using TyriaPlanner.Hud.Ui;
namespace TyriaPlanner.Hud.Services
{
    public sealed class NotificationService
    {
        private static readonly Logger Logger = Logger.GetLogger<NotificationService>();
        private readonly ToastStack _stack;
        private readonly ModuleSettings _settings;
        private readonly NotificationHistory _history;
        public NotificationService(ToastStack stack, ModuleSettings settings, NotificationHistory history)
        {
            _stack = stack;
            _settings = settings;
            _history = history;
        }
        private void RecordAndChime(string title, string subtitle, string eventType, string eventId)
        {
            _history.Record(title, subtitle, eventType, eventId);
            if (_settings.PlaySoundOnToast.Value)
            {
                try { SystemSounds.Asterisk.Play(); }
                catch (Exception ex) { Logger.Warn(ex, "System sound failed."); }
            }
        }
        public void PostCheckinOpenToast(MySignup signup)
        {
            var title = $"Check-in time Â· {Pretty(signup.Title, signup.Type)}";
            var subtitle = "Open the Tyria Planner app and check in";
            RecordAndChime(title, subtitle, signup.Type, signup.Id);
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.Reminder,
                eventType: signup.Type,
                commanderAccountName: signup.CommanderAccountName,
                voiceChannelUrl: signup.VoiceChannelUrl,
                eventId: signup.Id,
                eventBaseUrl: BuildEventUrl(signup.Id),
                showSqjoin: true,
                showJoinFromAppHint: false,
                isRecurring: signup.IsRecurring,
                onSnooze: minutes => ScheduleSnooze(minutes, () => PostCheckinOpenToast(signup))));
        }
        public void PostStartingToast(MySignup signup)
        {
            var title = $"Starting now Â· {Pretty(signup.Title, signup.Type)}";
            var subtitle = signup.GuildName != null
                ? $"{signup.GuildName} Â· commander {Commander(signup)}"
                : $"Public Â· commander {Commander(signup)}";
            RecordAndChime(title, subtitle, signup.Type, signup.Id);
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.Reminder,
                eventType: signup.Type,
                commanderAccountName: signup.CommanderAccountName,
                voiceChannelUrl: signup.VoiceChannelUrl,
                eventId: signup.Id,
                eventBaseUrl: BuildEventUrl(signup.Id),
                showSqjoin: true,
                showJoinFromAppHint: false,
                isRecurring: signup.IsRecurring,
                onSnooze: minutes => ScheduleSnooze(minutes, () => PostStartingToast(signup))));
        }
        public void PostNewGuildEventToast(NewGuildEvent ev)
        {
            var title = $"New event Â· {Pretty(ev.Title, ev.Type)}";
            var when = (ev.ScheduledAt - DateTime.UtcNow).TotalMinutes;
            var subtitle = ev.GuildName != null
                ? $"{ev.GuildName} Â· in {FormatRelative(when)} Â· {Commander(ev)}"
                : $"in {FormatRelative(when)} Â· {Commander(ev)}";
            RecordAndChime(title, subtitle, ev.Type, ev.Id);
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.NewEvent,
                eventType: ev.Type,
                commanderAccountName: ev.CommanderAccountName,
                voiceChannelUrl: ev.VoiceChannelUrl,
                eventId: ev.Id,
                eventBaseUrl: BuildEventUrl(ev.Id),
                showSqjoin: false,
                showJoinFromAppHint: true,
                isRecurring: ev.IsRecurring,
                onSnooze: null));
        }
        private static void ScheduleSnooze(int minutes, Action repost)
        {
            if (minutes <= 0) { repost(); return; }
            System.Threading.Timer t = null;
            t = new System.Threading.Timer(_ =>
            {
                GameService.Overlay.QueueMainThreadUpdate(__ => repost());
                t?.Dispose();
            }, null, TimeSpan.FromMinutes(minutes), System.Threading.Timeout.InfiniteTimeSpan);
        }
        private static string Pretty(string title, string type)
        {
            if (!string.IsNullOrWhiteSpace(title)) return title;
            switch (type)
            {
                case "raid":      return "Raid";
                case "strike":    return "Strike";
                case "fractal":   return "Fractal";
                case "wvw":       return "WvW";
                case "open_world":return "Open World";
                default:          return type ?? "Event";
            }
        }
        private static string Commander(EventBase ev)
        {
            if (!string.IsNullOrWhiteSpace(ev.CommanderAccountName)) return ev.CommanderAccountName;
            if (!string.IsNullOrWhiteSpace(ev.CommanderDisplayName)) return ev.CommanderDisplayName;
            return ev.CommanderUsername ?? "?";
        }
        private static string FormatRelative(double minutes)
        {
            if (minutes < 1) return "now";
            if (minutes < 60) return ((int)Math.Round(minutes)) + "m";
            if (minutes < 24 * 60)
            {
                int h = (int)(minutes / 60);
                int m = (int)Math.Round(minutes - h * 60);
                if (m >= 60) { h += 1; m = 0; }
                return m > 0 ? $"{h}h {m}m" : $"{h}h";
            }
            int d = (int)(minutes / (24 * 60));
            int rh = (int)Math.Round((minutes - d * 24 * 60) / 60);
            if (rh >= 24) { d += 1; rh = 0; }
            return rh > 0 ? $"{d}d {rh}h" : $"{d}d";
        }
        private static string BuildEventUrl(string id)
        {
            return $"https://tyriaplanner.com/event/{id}";
        }
    }
}
