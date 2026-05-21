using System;
using System.Collections.Generic;
using System.Linq;
using TyriaPlanner.Hud.Ui;
namespace TyriaPlanner.Hud.Services
{
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
