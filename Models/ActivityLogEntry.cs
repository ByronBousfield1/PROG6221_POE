using System;

namespace CyberAwarenessBot.Models
{
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;

        public string Display => $"[{Timestamp:HH:mm:ss}]  {Description}";
    }
}
