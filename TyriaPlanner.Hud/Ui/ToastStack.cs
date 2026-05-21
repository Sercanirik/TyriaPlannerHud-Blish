using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
namespace TyriaPlanner.Hud.Ui
{
    public sealed class ToastStack
    {
        private const int TopMargin = 36;
        private const int Spacing = 8;
        private const int ToastWidth = 380;
        private const int MaxVisible = 4;
        private readonly List<EventToast> _toasts = new List<EventToast>();
        public void Push(EventToast toast)
        {
            GameService.Overlay.QueueMainThreadUpdate(_ => Show(toast));
        }
        private void Show(EventToast toast)
        {
            var screen = GameService.Graphics.SpriteScreen;
            toast.Parent = screen;
            toast.Width = ToastWidth;
            toast.Disposed += OnToastDisposed;
            _toasts.Add(toast);
            if (_toasts.Count > MaxVisible)
            {
                var dropped = _toasts[0];
                _toasts.RemoveAt(0);
                dropped.Dispose();
            }
            ReflowLocation(screen);
        }
        private void OnToastDisposed(object sender, System.EventArgs e)
        {
            if (sender is EventToast t)
            {
                _toasts.Remove(t);
                ReflowLocation(GameService.Graphics.SpriteScreen);
            }
        }
        private void ReflowLocation(Control screen)
        {
            if (screen == null) return;
            var x = (screen.Width - ToastWidth) / 2;
            var y = TopMargin;
            foreach (var t in _toasts)
            {
                t.Location = new Point(x, y);
                y += t.Height + Spacing;
            }
        }
        public void Clear()
        {
            foreach (var t in _toasts.ToArray())
            {
                t.Dispose();
            }
            _toasts.Clear();
        }
    }
}
