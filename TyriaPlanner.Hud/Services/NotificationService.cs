using System;
using System.Diagnostics;
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
        public NotificationService(ToastStack stack, ModuleSettings settings)
        {
            _stack = stack;
            _settings = settings;
        }
        public void PostSignupReminder(MySignup signup, int minutesBefore)
        {
            string title;
            if (minutesBefore <= 0)        title = $"Starting now Â· {Pretty(signup.Title, signup.Type)}";
            else if (minutesBefore == 1)   title = $"Starting in 1 min Â· {Pretty(signup.Title, signup.Type)}";
            else                            title = $"{minutesBefore} min Â· {Pretty(signup.Title, signup.Type)}";
            var subtitle = signup.GuildName != null
                ? $"{signup.GuildName} Â· commander {Commander(signup)}"
                : $"Public Â· commander {Commander(signup)}";
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.Reminder,
                eventType: signup.Type,
                commanderAccountName: signup.CommanderAccountName,
                eventId: signup.Id,
                eventBaseUrl: BuildEventUrl(signup.Id),
                showSqjoin: true,                  
                showJoinFromAppHint: false));
        }
        public void PostSignedUpToast(MySignup signup)
        {
            var minutes = (signup.ScheduledAt - DateTime.UtcNow).TotalMinutes;
            string when;
            if (minutes < 0) when = "started";
            else if (minutes < 60) when = $"in {(int)Math.Max(0, Math.Round(minutes))} min";
            else if (minutes < 24 * 60) when = $"in {(minutes / 60.0):0.#} h";
            else when = $"in {(int)(minutes / 60 / 24)} d";
            var title = $"Signed up Â· {Pretty(signup.Title, signup.Type)}";
            var subtitle = signup.GuildName != null
                ? $"{signup.GuildName} Â· {when} Â· commander {Commander(signup)}"
                : $"Public Â· {when} Â· commander {Commander(signup)}";
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.Reminder,
                eventType: signup.Type,
                commanderAccountName: signup.CommanderAccountName,
                eventId: signup.Id,
                eventBaseUrl: BuildEventUrl(signup.Id),
                showSqjoin: true,
                showJoinFromAppHint: false));
        }
        public void PostCheckinOpenToast(MySignup signup)
        {
            var title = $"Check-in open Â· {Pretty(signup.Title, signup.Type)}";
            var subtitle = signup.GuildName != null
                ? $"{signup.GuildName} Â· mark yourself ready in the app"
                : "Mark yourself ready in the app";
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.Reminder,
                eventType: signup.Type,
                commanderAccountName: signup.CommanderAccountName,
                eventId: signup.Id,
                eventBaseUrl: BuildEventUrl(signup.Id),
                showSqjoin: true,
                showJoinFromAppHint: false));
        }
        public void PostNewGuildEventToast(NewGuildEvent ev)
        {
            var title = $"New event Â· {Pretty(ev.Title, ev.Type)}";
            var when = (ev.ScheduledAt - DateTime.UtcNow).TotalMinutes;
            var subtitle = ev.GuildName != null
                ? $"{ev.GuildName} Â· in {FormatRelative(when)} Â· {Commander(ev)}"
                : $"in {FormatRelative(when)} Â· {Commander(ev)}";
            _stack.Push(new EventToast(
                settings: _settings,
                title: title,
                subtitle: subtitle,
                accent: ToastAccent.NewEvent,
                eventType: ev.Type,
                commanderAccountName: ev.CommanderAccountName,
                eventId: ev.Id,
                eventBaseUrl: BuildEventUrl(ev.Id),
                showSqjoin: false,                 
                showJoinFromAppHint: true));
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
            if (minutes < 60) return ((int)Math.Round(minutes)) + "m";
            var hours = minutes / 60.0;
            if (hours < 24) return hours.ToString("0.#") + "h";
            return ((int)(hours / 24)) + "d";
        }
        private static string BuildEventUrl(string id)
        {
            return $"https://tyriaplanner.com/event/{id}";
        }
    }
}
