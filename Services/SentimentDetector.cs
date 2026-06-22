using System;
using System.Collections.Generic;

namespace CyberAwarenessBot.Services
{
    public class SentimentDetector
    {
        private readonly Dictionary<string, string[]> sentimentKeywords =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["worried"]    = new[] { "worried", "worry", "scared", "afraid", "anxious", "nervous", "concerned", "panic" },
                ["frustrated"] = new[] { "frustrated", "annoyed", "angry", "upset", "hate", "stuck", "tired of" },
                ["curious"]    = new[] { "curious", "wondering", "tell me", "explain", "interested in", "learn" },
                ["confused"]   = new[] { "confused", "don't understand", "dont understand", "lost", "unclear" },
                ["happy"]      = new[] { "thanks", "thank you", "great", "awesome", "cool", "love", "happy", "perfect", "nice" },
                ["sad"]        = new[] { "sad", "unhappy", "depressed", "down", "lonely", "hopeless" }
            };

        public delegate string SentimentResponseBuilder(string topic);

        private readonly Dictionary<string, SentimentResponseBuilder> responseBuilders;

        public SentimentDetector()
        {
            responseBuilders = new Dictionary<string, SentimentResponseBuilder>
            {
                ["worried"]    = topic => $"It's completely understandable to feel that way about {topic}. Scammers and attackers can be very convincing. Let me share a tip to help you stay safe.",
                ["frustrated"] = topic => $"I hear you. Cybersecurity can feel overwhelming. Let's take it one step at a time with {topic}.",
                ["curious"]    = topic => $"Good question. Here's something useful about {topic}:",
                ["confused"]   = topic => $"No problem. Let me explain {topic} in a simpler way.",
                ["happy"]      = topic => $"Glad you're finding this useful. Here's another note about {topic}:",
                ["sad"]        = topic => $"Sorry you're feeling down. Staying informed about {topic} is one way to feel more in control online."
            };
        }

        public string? Detect(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string lower = input.ToLower();

            string? bestMatch = null;
            int bestLength = 0;

            foreach (var pair in sentimentKeywords)
            {
                foreach (var keyword in pair.Value)
                {
                    if (lower.Contains(keyword) && keyword.Length > bestLength)
                    {
                        bestMatch = pair.Key;
                        bestLength = keyword.Length;
                    }
                }
            }
            return bestMatch;
        }

        public string BuildEmpatheticLead(string sentiment, string topic)
        {
            if (responseBuilders.TryGetValue(sentiment, out var builder))
            {
                return builder(topic);
            }
            return string.Empty;
        }

        public IEnumerable<string> KnownSentiments => sentimentKeywords.Keys;
    }
}
