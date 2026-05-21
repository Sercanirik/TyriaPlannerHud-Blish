using System;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Controls.Intern;
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
                await Task.Delay(60).ConfigureAwait(false);
                Keyboard.Stroke(VirtualKeyShort.RETURN);
                await Task.Delay(90).ConfigureAwait(false);
                Keyboard.Press(VirtualKeyShort.LCONTROL);
                Keyboard.Stroke(VirtualKeyShort.KEY_V);
                Keyboard.Release(VirtualKeyShort.LCONTROL);
                await Task.Delay(120).ConfigureAwait(false);
                Keyboard.Stroke(VirtualKeyShort.TAB);
                await Task.Delay(60).ConfigureAwait(false);
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
