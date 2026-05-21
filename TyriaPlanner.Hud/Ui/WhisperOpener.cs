using System;
using System.Threading.Tasks;
using Blish_HUD;
namespace TyriaPlanner.Hud.Ui
{
    public static class WhisperOpener
    {
        public static Task OpenAsync(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return Task.CompletedTask;
            return Task.Run(() =>
            {
                try
                {
                    GameService.GameIntegration.Chat.Paste($"/w {accountName} ");
                }
                catch (Exception ex)
                {
                    Logger.GetLogger(typeof(WhisperOpener)).Warn(ex, "Whisper paste failed for {0}", accountName);
                    try { Clipboard.Set($"/w {accountName} "); } catch { }
                }
            });
        }
    }
}
