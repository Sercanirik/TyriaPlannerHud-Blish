using System;
using System.Collections.Generic;
using System.Linq;
using TyriaPlanner.Hud.Ui;

namespace TyriaPlanner.Hud.Services
{
    // Ring buffer of recent toast events. The menu's "History" section reads
    // from here so users can see what toasts they may have missed (closed
    // GW2 while a check-in was open, etc.). In-memory only · cleared when
    // the module unloads.
    public sealed class NotificationHistory
    {
        private const int Capacity = 30;
        private readonly LinkedList<HistoryEntry> _entries = new LinkedList<HistoryEntry>();
        private readonly object _lock = new object();

        public void Record(string title, string subtitle, string eventType, string eventId)
        {
            lock (_lock)
            {
                _entries.AddFirst(new HistoryEntry
                {
                    Title = title,
                    Subtitle = subtitle,
                    EventType = eventType,
                    EventId = eventId,
                    At = DateTime.UtcNow,
                });
                while (_entries.Count > Capacity) _entries.RemoveLast();
            }
        }

        public List<HistoryEntry> Snapshot()
        {
            lock (_lock) { return _entries.ToList(); }
        }

        public void Remove(HistoryEntry entry)
        {
            if (entry == null) return;
            lock (_lock)
            {
                // Reference-based remove · the snapshot returns the same
                // instances so the menu's dismiss button hands us back the
                // exact node we stored.
                _entries.Remove(entry);
            }
        }
    }

    public sealed class HistoryEntry
    {
        public string Title;
        public string Subtitle;
        public string EventType;
        public string EventId;
        public DateTime At;
    }
}
