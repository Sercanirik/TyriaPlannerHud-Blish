using System;
using System.Collections.Generic;
using System.Threading;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using TyriaPlanner.Hud.Settings;

namespace TyriaPlanner.Hud.Ui
{
    // Generic toast container stack. Accepts any Blish HUD `Container` so we
    // can push both EventToast and AnnouncementToast through the same
    // positioning / combat-pause / max-visible pipeline.
    public sealed class ToastStack : IDisposable
    {
        public event Action<Container> ToastPushed;

        private const int Margin = 12;
        private const int TopBarClearance = 36;
        private const int Spacing = 8;
        private const int ToastWidth = 380;
        private const int MaxVisible = 4;

        private readonly List<Container> _toasts = new List<Container>();
        // Deferred queue · holds toast factories waiting for the player to
        // leave combat. We store the factory not the toast itself so the
        // control is constructed on the main thread when it is actually shown
        // (Blish controls don't tolerate cross-thread init).
        private readonly Queue<Func<Container>> _deferred = new Queue<Func<Container>>();
        private readonly ModuleSettings _settings;
        private readonly Timer _combatPoller;

        public ToastStack(ModuleSettings settings)
        {
            _settings = settings;
            // Poll combat state every second · cheap (Mumble is just a memory
            // map). Drains the deferred queue as soon as combat ends.
            _combatPoller = new Timer(_ => TryDrainDeferred(), null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void Push(Func<Container> factory)
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

        // Convenience for call sites that already constructed the toast.
        public void Push(Container toast)
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
            List<Func<Container>> drained;
            lock (_deferred)
            {
                if (_deferred.Count == 0) return;
                drained = new List<Func<Container>>(_deferred);
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

        private void Show(Container toast)
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
            if (sender is Container t)
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
