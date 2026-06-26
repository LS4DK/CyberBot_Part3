using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberBot_Part3.Logic
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }   // Task | Reminder | Quiz | NLP | Chat
        public string Description { get; set; }

        public override string ToString()
            => $"[{Timestamp:HH:mm:ss}] [{Category}] {Description}";
    }

    public static class ActivityLog
    {
        private static readonly List<LogEntry> _entries = new List<LogEntry>();

        public static void Add(string category, string description)
        {
            _entries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Category = category,
                Description = description
            });
        }

        /// <summary>Returns the most recent <paramref name="count"/> entries.</summary>
        public static List<LogEntry> GetRecent(int count = 10)
            => _entries.AsEnumerable().Reverse().Take(count).ToList();

        /// <summary>Returns ALL entries.</summary>
        public static List<LogEntry> GetAll()
            => _entries.AsEnumerable().Reverse().ToList();

        public static int TotalCount => _entries.Count;
    }
}
