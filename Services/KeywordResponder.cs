using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberAwarenessBot.Services
{
    public class KeywordResponder
    {
        private readonly Dictionary<string, List<string>> topicKeywords;
        private readonly Dictionary<string, List<string>> topicResponses;

        private readonly List<string> continuationPhrases = new List<string>
        {
            "more", "another", "tell me more", "explain more", "go on",
            "another tip", "next", "continue", "more info", "more please",
            "again", "elaborate"
        };

        private readonly Random random = new Random();

        public KeywordResponder()
        {
            topicKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"]      = new List<string> { "password", "passwords", "passcode", "credentials", "login details" },
                ["phishing"]      = new List<string> { "phish", "phishing", "fake email", "suspicious email", "spoof" },
                ["scam"]          = new List<string> { "scam", "scammer", "fraud", "fraudster", "419", "smishing", "vishing" },
                ["privacy"]       = new List<string> { "privacy", "private", "personal data", "personal info", "popi", "popia", "data protection" },
                ["safe browsing"] = new List<string> { "brows", "browser", "https", "website safety", "url" },
                ["malware"]       = new List<string> { "malware", "virus", "ransomware", "trojan", "spyware", "worm" },
                ["two-factor authentication"] = new List<string> { "2fa", "two factor", "two-factor", "mfa", "authenticator" },
                ["public wifi"]   = new List<string> { "wifi", "wi-fi", "public network", "hotspot" },
                ["social media"]  = new List<string> { "social media", "facebook", "instagram", "twitter", "tiktok", "whatsapp" },
                ["identity theft"] = new List<string> { "identity theft", "id theft", "stolen identity", "impersonation" }
            };

            topicResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = new List<string>
                {
                    "Use a mix of uppercase, lowercase, numbers, and symbols. Make passwords at least 12 characters long.",
                    "Never reuse the same password across different sites. If one site is breached, all your accounts become vulnerable.",
                    "Use a password manager like Bitwarden or 1Password instead of writing passwords on paper or in a notes app.",
                    "Turn on Two-Factor Authentication (2FA) wherever possible. It adds a strong extra layer of protection.",
                    "Passphrases like 'PurpleZebra-Eats-Sunshine!' are easier to remember and harder to crack than short complex passwords."
                },
                ["phishing"] = new List<string>
                {
                    "Be careful of emails asking for personal information. Scammers often pretend to be trusted organisations like FNB, SARS, or Eskom.",
                    "Always check the sender's email address carefully. Phishers often use slight misspellings like 'fnb-support.co' instead of 'fnb.co.za'.",
                    "Never click suspicious links. Hover over a link first to preview the actual URL before clicking.",
                    "Watch out for urgent language demanding immediate action. 'Your account will be closed in 24 hours!' is a classic phishing tactic.",
                    "If an email asks for your OTP or banking password, it is almost certainly phishing. Banks never ask for these."
                },
                ["scam"] = new List<string>
                {
                    "If a deal sounds too good to be true, like a 'You've won R500,000!' SMS, it almost always is. Delete it.",
                    "Never send money or airtime to someone you've only met online, no matter how compelling their story is.",
                    "Watch out for romance scams on dating apps where someone quickly asks for financial help.",
                    "SMS scams often pretend to be from delivery services like the Post Office or Takealot. Don't click unexpected tracking links.",
                    "Report scams to the South African Banking Risk Information Centre (SABRIC) or the SAPS Cybercrime unit."
                },
                ["privacy"] = new List<string>
                {
                    "Review the privacy settings on your social media accounts regularly. Limit who can see your posts and personal info.",
                    "Be careful what you share publicly. Things like your full date of birth, ID number, and address can be used for identity theft.",
                    "Under South Africa's POPIA law, you have the right to know what personal data companies hold about you and to request its deletion.",
                    "Use a private browser window when shopping or accessing sensitive accounts on shared devices.",
                    "Turn off location sharing on apps that don't really need it. Many apps track your location even when you're not using them."
                },
                ["safe browsing"] = new List<string>
                {
                    "Always look for the padlock icon and 'https://' in the address bar before entering any sensitive information on a website.",
                    "Avoid downloading software from untrusted sources. Stick to official app stores and verified vendor websites.",
                    "Keep your browser, operating system, and antivirus software updated to patch security vulnerabilities.",
                    "Be careful when using public Wi-Fi. Avoid logging into banking apps or entering passwords on unsecured networks.",
                    "Install a reputable ad-blocker. Many malicious ads can compromise your device with a single click."
                },
                ["malware"] = new List<string>
                {
                    "Keep your antivirus software updated and run regular scans. Microsoft Defender is a solid free option on Windows.",
                    "Don't open email attachments from unknown senders, especially .exe, .zip, or .scr files.",
                    "Back up your important files regularly to an external drive or cloud service. This is your best defence against ransomware.",
                    "Avoid pirated software and cracked downloads. They are a leading source of malware infections.",
                    "If your device suddenly slows down, shows pop-ups, or your browser homepage changes by itself, you may be infected. Run a full scan."
                },
                ["two-factor authentication"] = new List<string>
                {
                    "2FA adds a second lock to your accounts. Even if a hacker has your password, they can't log in without the second code.",
                    "Use an authenticator app like Google Authenticator or Microsoft Authenticator rather than SMS. SIM swap fraud is common in South Africa.",
                    "Turn on 2FA for your most important accounts first: email, banking, WhatsApp, and social media.",
                    "Save your 2FA backup codes in a safe place. If you lose your phone, these codes can help you recover access."
                },
                ["public wifi"] = new List<string>
                {
                    "Avoid logging into your bank or entering passwords on public Wi-Fi. Attackers can intercept the traffic.",
                    "Use a reputable VPN when connecting to public Wi-Fi at cafes, airports, or shopping malls.",
                    "Turn off automatic Wi-Fi connection on your phone so it doesn't silently join malicious hotspots.",
                    "If you must use public Wi-Fi, stick to browsing. Never do online banking or shopping on it."
                },
                ["social media"] = new List<string>
                {
                    "Don't accept friend requests from strangers. Scammers create fake profiles to gather information for targeted attacks.",
                    "Check your social media privacy settings every few months. Platforms update them often, sometimes resetting defaults.",
                    "Think before you post. Anything you put online can be screenshotted, saved, and used against you later.",
                    "Be careful of fun quizzes asking your first pet's name or street you grew up on. These are common security question answers."
                },
                ["identity theft"] = new List<string>
                {
                    "Never share your South African ID number unless absolutely necessary, and never over email or SMS.",
                    "Shred documents containing personal details before throwing them away.",
                    "Check your credit report annually with bureaus like TransUnion or Experian to spot suspicious accounts opened in your name.",
                    "If your ID is lost or stolen, report it to SAPS immediately and register it with the Southern African Fraud Prevention Service (SAFPS)."
                }
            };
        }

        public string? DetectTopic(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string lower = input.ToLower();

            foreach (var pair in topicKeywords)
            {
                foreach (var keyword in pair.Value)
                {
                    if (lower.Contains(keyword))
                    {
                        return pair.Key;
                    }
                }
            }
            return null;
        }

        public string GetRandomResponse(string topic, string? lastResponse = null)
        {
            if (!topicResponses.TryGetValue(topic, out var responses) || responses.Count == 0)
            {
                return "I have information on that topic, but no tips loaded yet.";
            }

            if (responses.Count == 1) return responses[0];

            string pick;
            int safety = 0;
            do
            {
                pick = responses[random.Next(responses.Count)];
                safety++;
            } while (pick == lastResponse && safety < 5);

            return pick;
        }

        public bool IsContinuation(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string lower = input.ToLower().Trim();
            return continuationPhrases.Any(p => lower.Contains(p));
        }

        public IEnumerable<string> AllTopics => topicResponses.Keys;
    }
}
