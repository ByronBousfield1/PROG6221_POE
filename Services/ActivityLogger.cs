using System;
using System.Collections.Generic;
using System.Linq;
using CyberAwarenessBot.Models;

namespace CyberAwarenessBot.Services
{
    public class ActivityLogger
    {
        private readonly List<ActivityLogEntry> entries = new List<ActivityLogEntry>();

        public event Action? Changed;

        public void Log(string description)
        {
            entries.Add(new ActivityLogEntry
            {
                Timestamp = DateTime.Now,
                Description = description
            });
            Changed?.Invoke();
        }

        public List<ActivityLogEntry> GetRecent(int count = 10)
        {
            int skip = Math.Max(0, entries.Count - count);
            return entries.Skip(skip).Reverse().ToList();
        }

        public List<ActivityLogEntry> GetAll()
        {
            return entries.AsEnumerable().Reverse().ToList();
        }

        public int Count => entries.Count;
    }
}
