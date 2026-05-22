using System;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;

namespace TyriaPlanner.Hud.Ui
{
    public static class Clipboard
    {
        public static void Set(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                _ = ClipboardUtil.WindowsClipboardService
                    .SetTextAsync(text)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Logger.GetLogger(typeof(Clipboard))
                                .Warn(t.Exception, "Clipboard write failed.");
                        }
                    }, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                Logger.GetLogger(typeof(Clipboard)).Warn(ex, "Clipboard write threw.");
            }
        }

        // Sets `text` on the clipboard, then restores whatever was there
        // before after `restoreAfter`. Used by command-copy buttons so we
        // don't leave "/sqjoin Name" sitting in the user's clipboard once
        // they've actually pasted it into chat.
        public static async Task SetAndRestoreAsync(string text, TimeSpan restoreAfter)
        {
            if (string.IsNullOrEmpty(text)) return;
            string previous = string.Empty;
            try { previous = await ClipboardUtil.WindowsClipboardService.GetTextAsync().ConfigureAwait(false); } catch { }
            try { await ClipboardUtil.WindowsClipboardService.SetTextAsync(text).ConfigureAwait(false); } catch { }
            try { await Task.Delay(restoreAfter).ConfigureAwait(false); } catch { }
            try { await ClipboardUtil.WindowsClipboardService.SetTextAsync(previous ?? string.Empty).ConfigureAwait(false); } catch { }
        }
    }
}
