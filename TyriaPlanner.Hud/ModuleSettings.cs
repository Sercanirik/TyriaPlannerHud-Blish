using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace TyriaPlanner.Hud.Settings
{
    public enum FontSizePreference
    {
        Small,
        Medium,
        Large,
    }

    public enum ToastPositionPreference
    {
        TopCenter,
        TopRight,
        BottomRight,
    }

    public enum ColorThemePreference
    {
        Default,
        HighContrast,
        Pastel,
        Monochrome,
    }

    public sealed class ModuleSettings
    {
        public SettingCollection Root { get; }
        public SettingEntry<string> ApiBaseUrl { get; }
        public SettingEntry<string> Gw2ApiKey { get; }
        public SettingEntry<string> CachedBearer { get; }       // hidden from UI
        public SettingEntry<bool>   NotifyOwnSignups { get; }
        public SettingEntry<bool>   NotifyNewGuildEvents { get; }
        public SettingEntry<bool>   NotifyGuildAnnouncements { get; }
        public SettingEntry<int>    PollIntervalSeconds { get; }
        public SettingEntry<FontSizePreference> FontSize { get; }
        public SettingEntry<ToastPositionPreference> ToastPosition { get; }
        public SettingEntry<bool> PlaySoundOnToast { get; }
        public SettingEntry<bool> NotifyWeeklyReset { get; }
        public SettingEntry<bool> PauseInCombat { get; }
        public SettingEntry<ColorThemePreference> ColorTheme { get; }

        public ModuleSettings(SettingCollection root)
        {
            Root = root;
            ApiBaseUrl = root.DefineSetting(
                "ApiBaseUrl",
                "https://tyriaplanner.com",
                () => "API base URL",
                () => "Leave the default unless you're testing against a dev server.");

            Gw2ApiKey = root.DefineSetting(
                "Gw2ApiKey",
                string.Empty,
                () => "GW2 API key",
                () => "Must be the SAME GW2 API key you already saved on your tyriaplanner.com profile · the server only recognises keys it has on file. Required ArenaNet permissions: account, characters, progression. Optional: builds, wallet, inventories (for the full website experience). Generate at https://account.arena.net/applications. The addon sends this key to Tyria Planner exactly once · after that, every poll uses a scoped, revocable bearer.");

            CachedBearer = root.DefineSetting(
                "CachedBearer",
                string.Empty,
                null,
                null);

            NotifyOwnSignups = root.DefineSetting(
                "NotifyOwnSignups",
                true,
                () => "Notify upcoming signups",
                () => "Pops a toast for events you signed up to as they approach.");

            NotifyNewGuildEvents = root.DefineSetting(
                "NotifyNewGuildEvents",
                true,
                () => "Notify new guild events",
                () => "Pops a toast when one of your guilds posts a new event.");

            NotifyGuildAnnouncements = root.DefineSetting(
                "NotifyGuildAnnouncements",
                true,
                () => "Notify guild announcements",
                () => "Pops a toast when an owner or officer of one of your guilds posts an announcement.");

            PollIntervalSeconds = root.DefineSetting(
                "PollIntervalSeconds",
                45,
                () => "Poll interval (seconds)",
                () => "How often the module checks for updates. 30-120 is sane; under 30 may rate-limit.");
            PollIntervalSeconds.SetRange(30, 300);

            FontSize = root.DefineSetting(
                "FontSize",
                FontSizePreference.Medium,
                () => "Font size",
                () => "Bumps all text in the menu and toasts by one or two notches. Useful on 4K or for far-away seating.");

            ToastPosition = root.DefineSetting(
                "ToastPosition",
                ToastPositionPreference.TopCenter,
                () => "Toast position",
                () => "Where notification toasts pile up on screen.");

            PlaySoundOnToast = root.DefineSetting(
                "PlaySoundOnToast",
                true,
                () => "Play sound on toast",
                () => "Short chime when a check-in or starting reminder appears.");

            NotifyWeeklyReset = root.DefineSetting(
                "NotifyWeeklyReset",
                true,
                () => "Weekly raid reset reminder",
                () => "One toast on Monday 07:30 UTC reminding you the raid week reset (refresh your kill-proofs).");

            PauseInCombat = root.DefineSetting(
                "PauseInCombat",
                false,
                () => "Hold notifications in combat",
                () => "Defers toasts while you're in combat (Mumble flag). They fire as soon as combat ends so you don't lose them.");

            ColorTheme = root.DefineSetting(
                "ColorTheme",
                ColorThemePreference.Default,
                () => "Color theme",
                () => "Recolours the per-type accent (raid/strike/fractal/wvw/open-world) in menu rows and toasts.");
        }

        // Title font · used for event titles, section headers, toast titles.
        public BitmapFont TitleFont()
        {
            switch (FontSize.Value)
            {
                case FontSizePreference.Small:  return GameService.Content.DefaultFont14;
                case FontSizePreference.Large:  return GameService.Content.DefaultFont18;
                default:                         return GameService.Content.DefaultFont16;
            }
        }

        // Body font · used for subtitles, descriptions, button labels.
        public BitmapFont BodyFont()
        {
            switch (FontSize.Value)
            {
                case FontSizePreference.Small:  return GameService.Content.DefaultFont12;
                case FontSizePreference.Large:  return GameService.Content.DefaultFont16;
                default:                         return GameService.Content.DefaultFont14;
            }
        }
    }
}
