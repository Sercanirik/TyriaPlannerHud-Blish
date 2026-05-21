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
            var command = $"/w {accountName} ";
            return Task.Run(() =>
            {
                var log = Logger.GetLogger(typeof(WhisperOpener));
                try { Clipboard.Set(command); }
                catch (Exception ex) { log.Warn(ex, "clipboard pre-set failed"); }
                bool gw2HasFocus = false;
                try { gw2HasFocus = GameService.GameIntegration.Gw2Instance.Gw2HasFocus; } catch { }
                log.Info("whisper paste Â· target={0} gw2HasFocus={1}", accountName, gw2HasFocus);
                try
                {
                    GameService.GameIntegration.Chat.Paste(command);
                    log.Info("whisper paste invoked");
                }
                catch (Exception ex)
                {
                    log.Warn(ex, "GameIntegration.Chat.Paste threw for {0}", accountName);
                }
                try { Clipboard.Set(command); }
                catch (Exception ex) { log.Warn(ex, "clipboard post-set failed"); }
            });
        }
    }
}
