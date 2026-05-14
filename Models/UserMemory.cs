using System.Collections.Generic;

namespace CyberAwarenessBot.Models
{
    public class UserMemory
    {
        public string UserName { get; set; } = "Friend";
        public string? FavouriteTopic { get; set; }
        public string? LastTopic { get; set; }
        public string? LastSentiment { get; set; }

        public List<string> Interests { get; } = new List<string>();
        public HashSet<string> DiscussedTopics { get; } = new HashSet<string>();

        public void AddInterest(string topic)
        {
            if (!string.IsNullOrWhiteSpace(topic) && !Interests.Contains(topic))
            {
                Interests.Add(topic);
            }
        }

        public bool HasInterest(string topic) => Interests.Contains(topic);

        public void RememberTopic(string topic)
        {
            LastTopic = topic;
            DiscussedTopics.Add(topic);
        }
    }
}
