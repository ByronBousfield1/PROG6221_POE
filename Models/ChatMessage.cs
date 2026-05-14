using System;

namespace CyberAwarenessBot.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; } = "Bot";
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsUser { get; set; }
        public string Category { get; set; } = "general";
    }
}
