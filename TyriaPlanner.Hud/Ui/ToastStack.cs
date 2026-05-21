using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using TyriaPlanner.Hud.Settings;
namespace TyriaPlanner.Hud.Ui
{
    public sealed class ToastStack
    {
        public event Action<EventToast> ToastPushed;
        private const int Margin = 12;
        private const int TopBarClearance = 36;
        private const int Spacing = 8;
        private const int ToastWidth = 380;
        private const int MaxVisible = 4;
        private readonly List<EventToast> _toasts = new List<EventToast>();
        private readonly ModuleSettings _settings;
        public ToastStack(ModuleSettings settings)
        {
            _settings = settings;
        }
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
            ToastPushed?.Invoke(toast);
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
            var pos = _settings?.ToastPosition?.Value ?? ToastPositionPreference.TopCenter;
            switch (pos)
            {
                case ToastPositionPreference.TopRight:
                {
                    var x = screen.Width - ToastWidth - Margin;
                    var y = TopBarClearance;
                    foreach (var t in _toasts) { t.Location = new Point(x, y); y += t.Height + Spacing; }
                    break;
                }
                case ToastPositionPreference.BottomRight:
                {
                    var x = screen.Width - ToastWidth - Margin;
                    var y = screen.Height - Margin;
                    for (int i = _toasts.Count - 1; i >= 0; i--)
                    {
                        var t = _toasts[i];
                        y -= t.Height;
                        t.Location = new Point(x, y);
                        y -= Spacing;
                    }
                    break;
                }
                default: 
                {
                    var x = (screen.Width - ToastWidth) / 2;
                    var y = TopBarClearance;
                    foreach (var t in _toasts) { t.Location = new Point(x, y); y += t.Height + Spacing; }
                    break;
                }
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
