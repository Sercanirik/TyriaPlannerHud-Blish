using System;
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
    }
}
