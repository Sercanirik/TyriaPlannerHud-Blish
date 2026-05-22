using System;
using System.Diagnostics;
using Blish_HUD;

namespace TyriaPlanner.Hud.Ui
{
    public static class SafeUrl
    {
        public static bool IsAllowed(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            var s = uri.Scheme.ToLowerInvariant();
            return s == "http" || s == "https" || s == "discord" || s == "mumble";
        }

        public static void Open(string url)
        {
            if (!IsAllowed(url))
            {
                Logger.GetLogger(typeof(SafeUrl)).Warn("Refused to open non-whitelisted URL scheme: {0}", url);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.GetLogger(typeof(SafeUrl)).Warn(ex, "Browser launch failed for {0}", url);
            }
        }
    }
}
