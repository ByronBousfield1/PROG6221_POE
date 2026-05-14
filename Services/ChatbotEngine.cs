using System;
using System.Collections.Generic;
using System.Linq;
using CyberAwarenessBot.Models;

namespace CyberAwarenessBot.Services
{
    public class ChatbotEngine
    {
        public delegate string? ResponseStrategy(string input);

        private readonly List<ResponseStrategy> strategies;
        private readonly KeywordResponder responder;
        private readonly SentimentDetector sentimentDetector;
        public UserMemory Memory { get; }

        private readonly Dictionary<string, string> lastResponsePerTopic = new();

        public ChatbotEngine(UserMemory memory)
        {
            Memory = memory;
            responder = new KeywordResponder();
            sentimentDetector = new SentimentDetector();

            strategies = new List<ResponseStrategy>
            {
                TryHandleExit,
                TryHandleSmallTalk,
                TryHandleInterestStatement,
                TryHandleRecallRequest,
                TryHandleContinuation,
                TryHandleKeyword,
                TryHandleHelp,
                FallbackResponse
            };
        }

        public IEnumerable<string> AllTopics => responder.AllTopics;

        public string GetResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "I didn't catch that. Could you type something? You can ask about phishing, passwords, scams, or other cybersecurity topics.";
            }

            string? sentiment = sentimentDetector.Detect(input);
            if (sentiment != null) Memory.LastSentiment = sentiment;

            foreach (var strategy in strategies)
            {
                string? reply = strategy(input);
                if (!string.IsNullOrEmpty(reply))
                {
                    if (sentiment != null && Memory.LastTopic != null
                        && (sentiment == "worried" || sentiment == "frustrated" || sentiment == "confused"))
                    {
                        string lead = sentimentDetector.BuildEmpatheticLead(sentiment, Memory.LastTopic);
                        if (!string.IsNullOrEmpty(lead) && !reply.StartsWith(lead))
                        {
                            reply = lead + "\n\n" + reply;
                        }
                    }
                    return reply;
                }
            }

            return "I'm not sure I understand. Try rephrasing, or ask me about phishing, passwords, or safe browsing.";
        }

        private string? TryHandleExit(string input)
        {
            string lower = input.ToLower();
            if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("bye") || lower.Contains("goodbye"))
            {
                return $"Stay safe online, {Memory.UserName}! Remember: when in doubt, don't click. Goodbye!";
            }
            return null;
        }

        private string? TryHandleSmallTalk(string input)
        {
            string lower = input.ToLower();

            if (lower.Contains("how are you"))
                return "I'm running well and ready to help you stay safe online. How can I help today?";

            if (lower.Contains("purpose") || lower.Contains("who are you") || lower.Contains("what are you"))
                return "I'm the Cybersecurity Awareness Bot. My purpose is to help South African citizens identify and avoid cyber threats like phishing, scams, and identity theft.";

            if (lower.StartsWith("hi") || lower.StartsWith("hello") || lower.StartsWith("hey") || lower.Contains("good morning") || lower.Contains("good afternoon"))
                return $"Hello again, {Memory.UserName}. What would you like to learn about today?";

            if (lower.Contains("thank"))
                return "You're welcome. Got another cybersecurity question?";

            if (lower.Contains("your name"))
                return "I'm the Cybersecurity Awareness Bot. You can call me CyberBot.";

            return null;
        }

        private string? TryHandleInterestStatement(string input)
        {
            string lower = input.ToLower();
            string[] triggers = { "i'm interested in", "im interested in", "interested in", "i like", "i want to learn about", "i care about" };

            foreach (var trigger in triggers)
            {
                int idx = lower.IndexOf(trigger);
                if (idx >= 0)
                {
                    string? topic = responder.DetectTopic(input);
                    if (topic != null)
                    {
                        Memory.AddInterest(topic);
                        Memory.FavouriteTopic ??= topic;
                        Memory.RememberTopic(topic);

                        string tip = responder.GetRandomResponse(topic);
                        lastResponsePerTopic[topic] = tip;

                        return $"Got it, {Memory.UserName} — I'll remember that you're interested in {topic}. It's a crucial part of staying safe online.\n\nHere's something useful:\n{tip}";
                    }
                }
            }
            return null;
        }

        private string? TryHandleRecallRequest(string input)
        {
            string lower = input.ToLower();
            if (lower.Contains("remember") || lower.Contains("what did i") || lower.Contains("what do you know about me"))
            {
                if (Memory.Interests.Count == 0)
                {
                    return $"So far I know your name is {Memory.UserName}, but you haven't told me your favourite cybersecurity topic yet. Try saying 'I'm interested in privacy' or similar.";
                }

                string interestList = string.Join(", ", Memory.Interests);
                return $"I remember you're interested in: {interestList}. As someone interested in {Memory.FavouriteTopic}, you might want to review the security settings on your accounts.";
            }
            return null;
        }

        private string? TryHandleContinuation(string input)
        {
            if (responder.IsContinuation(input) && responder.DetectTopic(input) == null && Memory.LastTopic != null)
            {
                string topic = Memory.LastTopic;
                lastResponsePerTopic.TryGetValue(topic, out string? last);
                string tip = responder.GetRandomResponse(topic, last);
                lastResponsePerTopic[topic] = tip;

                return $"Here's another tip on {topic}:\n{tip}";
            }
            return null;
        }

        private string? TryHandleKeyword(string input)
        {
            string? topic = responder.DetectTopic(input);
            if (topic == null) return null;

            Memory.RememberTopic(topic);

            lastResponsePerTopic.TryGetValue(topic, out string? last);
            string tip = responder.GetRandomResponse(topic, last);
            lastResponsePerTopic[topic] = tip;

            string prefix = Memory.HasInterest(topic)
                ? $"As someone interested in {topic}, here's something worth knowing:\n"
                : $"Here's a tip on {topic}:\n";

            return prefix + tip;
        }

        private string? TryHandleHelp(string input)
        {
            string lower = input.ToLower();
            if (lower.Contains("help") || lower.Contains("what can i ask") || lower.Contains("what can you do") || lower.Contains("topics"))
            {
                string topics = string.Join(", ", responder.AllTopics);
                return $"You can ask me about any of these topics: {topics}.\n\nYou can also say things like:\n  - 'Tell me more' to continue a topic\n  - 'I'm interested in privacy' so I remember it\n  - 'What do you remember about me?'";
            }
            return null;
        }

        private string? FallbackResponse(string input)
        {
            string[] fallbacks =
            {
                "I'm not sure I understand. Can you try rephrasing? You can ask about phishing, passwords, scams, privacy, or safe browsing.",
                $"Hmm, I didn't catch that, {Memory.UserName}. Try asking 'What can I ask you about?' to see the topics I know.",
                "That's outside my knowledge area. I focus on cybersecurity topics. Try asking about malware, 2FA, or public Wi-Fi.",
                "I'm only trained on cybersecurity topics. Could you rephrase your question around online safety?"
            };
            var rng = new Random();
            return fallbacks[rng.Next(fallbacks.Length)];
        }
    }
}
