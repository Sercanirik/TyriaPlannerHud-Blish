using System;
using System.Threading.Tasks;
using Blish_HUD;
namespace TyriaPlanner.Hud.Ui
{
    public static class WhisperOpener
    {
        public static async Task OpenAsync(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return;
            string previous = string.Empty;
            try { previous = await ClipboardUtil.WindowsClipboardService.GetTextAsync().ConfigureAwait(false); } catch { }
            try
            {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync($"/w {accountName}").ConfigureAwait(false);
                await Task.Delay(70).ConfigureAwait(false);
                WindowsInput.Enter();
                await Task.Delay(110).ConfigureAwait(false);
                WindowsInput.CtrlV();
                await Task.Delay(140).ConfigureAwait(false);
                WindowsInput.Tab();
                await Task.Delay(70).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.GetLogger(typeof(WhisperOpener)).Warn(ex, "Whisper sequence failed for {0}", accountName);
            }
            finally
            {
                try { await ClipboardUtil.WindowsClipboardService.SetTextAsync(previous ?? string.Empty).ConfigureAwait(false); } catch { }
            }
        }
    }
}
