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
                await ClipboardUtil.WindowsClipboardService.SetTextAsync($"/w {accountName} ").ConfigureAwait(false);
                await Task.Delay(60).ConfigureAwait(false);
                if (!GameService.Gw2Mumble.UI.IsTextInputFocused)
                {
                    Keyboard.Stroke(VirtualKeyShort.RETURN);
                    for (int i = 0; i < 25 && !GameService.Gw2Mumble.UI.IsTextInputFocused; i++)
                    {
                        await Task.Delay(20).ConfigureAwait(false);
                    }
                }
                Keyboard.Press(VirtualKeyShort.CONTROL);
                await Task.Delay(20).ConfigureAwait(false);
                Keyboard.Stroke(VirtualKeyShort.KEY_V);
                await Task.Delay(20).ConfigureAwait(false);
                Keyboard.Release(VirtualKeyShort.CONTROL);
                await Task.Delay(80).ConfigureAwait(false);
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
