using System;
using System.Collections.Generic;
using System.Threading;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using TyriaPlanner.Hud.Settings;
namespace TyriaPlanner.Hud.Ui
{
    public sealed class ToastStack : IDisposable
    {
        public event Action<EventToast> ToastPushed;
        private const int Margin = 12;
        private const int TopBarClearance = 36;
        private const int Spacing = 8;
        private const int ToastWidth = 380;
        private const int MaxVisible = 4;
        private readonly List<EventToast> _toasts = new List<EventToast>();
        private readonly Queue<Func<EventToast>> _deferred = new Queue<Func<EventToast>>();
        private readonly ModuleSettings _settings;
        private readonly Timer _combatPoller;
        public ToastStack(ModuleSettings settings)
        {
            _settings = settings;
            _combatPoller = new Timer(_ => TryDrainDeferred(), null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
        public void Push(Func<EventToast> factory)
        {
            if (factory == null) return;
            if (ShouldDefer())
            {
                lock (_deferred) { _deferred.Enqueue(factory); }
                return;
            }
            GameService.Overlay.QueueMainThreadUpdate(_ =>
            {
                try { Show(factory()); } catch { }
            });
        }
        public void Push(EventToast toast)
        {
            if (toast == null) return;
            if (ShouldDefer())
            {
                lock (_deferred) { _deferred.Enqueue(() => toast); }
                return;
            }
            GameService.Overlay.QueueMainThreadUpdate(_ => Show(toast));
        }
        private bool ShouldDefer()
        {
            if (_settings?.PauseInCombat == null || !_settings.PauseInCombat.Value) return false;
            try { return GameService.Gw2Mumble.PlayerCharacter.IsInCombat; }
            catch { return false; }
        }
        private void TryDrainDeferred()
        {
            if (ShouldDefer()) return;
            List<Func<EventToast>> drained;
            lock (_deferred)
            {
                if (_deferred.Count == 0) return;
                drained = new List<Func<EventToast>>(_deferred);
                _deferred.Clear();
            }
            GameService.Overlay.QueueMainThreadUpdate(_ =>
            {
                foreach (var f in drained)
                {
                    try { Show(f()); } catch { }
                }
            });
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
            lock (_deferred) { _deferred.Clear(); }
        }
        public void Dispose()
        {
            _combatPoller?.Dispose();
            Clear();
        }
    }
}
