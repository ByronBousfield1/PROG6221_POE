using System;
using System.Collections.Generic;
using System.Linq;
using CyberAwarenessBot.Models;

namespace CyberAwarenessBot.Services
{
    public class QuizService
    {
        private List<QuizQuestion> questions;
        private int index;
        private readonly Random random = new Random();

        public int Score { get; private set; }
        public int CurrentNumber => index + 1;
        public int Total => questions.Count;
        public bool IsFinished => index >= questions.Count;
        public QuizQuestion Current => questions[index];

        public QuizService()
        {
            questions = BuildQuestions();
        }

        public void Restart()
        {
            index = 0;
            Score = 0;
            questions = questions.OrderBy(_ => random.Next()).ToList();
        }

        public bool Answer(int optionIndex)
        {
            bool correct = optionIndex == Current.CorrectIndex;
            if (correct) Score++;
            return correct;
        }

        public void Next()
        {
            index++;
        }

        public string FinalFeedback()
        {
            double percent = Total == 0 ? 0 : (double)Score / Total * 100;

            if (percent >= 80)
                return $"You scored {Score} out of {Total}. Great job, you're a cybersecurity pro!";
            if (percent >= 50)
                return $"You scored {Score} out of {Total}. Not bad, but keep learning to stay safe online.";

            return $"You scored {Score} out of {Total}. Cybersecurity takes practice. Try again and review the explanations.";
        }

        private List<QuizQuestion> BuildQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Options = new List<string> { "Reply with your password", "Delete the email", "Report the email as phishing", "Forward it to friends" },
                    CorrectIndex = 2,
                    Explanation = "Reporting phishing emails helps prevent scams. Legitimate organisations never ask for your password by email.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "A strong password should contain a mix of uppercase, lowercase, numbers, and symbols.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "True. Mixing character types makes a password much harder to crack.",
                    IsTrueFalse = true
                },
                new QuizQuestion
                {
                    Question = "Which of these is the safest way to store your passwords?",
                    Options = new List<string> { "On a sticky note on your monitor", "In a reputable password manager", "In a plain text file on your desktop", "Using the same password everywhere" },
                    CorrectIndex = 1,
                    Explanation = "A reputable password manager encrypts your passwords and lets you use a unique one for every site.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "It is safe to do online banking on free public Wi-Fi without a VPN.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 1,
                    Explanation = "False. Attackers can intercept traffic on public Wi-Fi. Use mobile data or a VPN for banking.",
                    IsTrueFalse = true
                },
                new QuizQuestion
                {
                    Question = "What does the padlock icon in your browser's address bar indicate?",
                    Options = new List<string> { "The site is government owned", "The connection uses HTTPS encryption", "The site cannot be hacked", "The site is free of ads" },
                    CorrectIndex = 1,
                    Explanation = "The padlock means the connection is encrypted with HTTPS, but it does not guarantee the site itself is trustworthy.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "Two-Factor Authentication (2FA) means you need two passwords for one account.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 1,
                    Explanation = "False. 2FA combines something you know (password) with something you have (a code or device).",
                    IsTrueFalse = true
                },
                new QuizQuestion
                {
                    Question = "You get an SMS saying you won a R500,000 prize and must click a link to claim it. What should you do?",
                    Options = new List<string> { "Click the link quickly before it expires", "Reply with your banking details", "Delete it, it is a scam", "Share it with family so they can also win" },
                    CorrectIndex = 2,
                    Explanation = "If it sounds too good to be true, it is. Unexpected prize messages are a common scam.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "Phishing is a type of attack that uses deceptive messages to trick you into revealing sensitive information.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "True. Phishing relies on deception, often pretending to be a trusted organisation.",
                    IsTrueFalse = true
                },
                new QuizQuestion
                {
                    Question = "Which is an example of social engineering?",
                    Options = new List<string> { "A firewall blocking traffic", "Someone phoning you pretending to be IT support to get your password", "An antivirus scan", "A software update" },
                    CorrectIndex = 1,
                    Explanation = "Social engineering manipulates people into giving up information, rather than hacking systems directly.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "Keeping your operating system and apps updated helps protect against malware.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "True. Updates patch security holes that attackers and malware exploit.",
                    IsTrueFalse = true
                },
                new QuizQuestion
                {
                    Question = "What is the best defence against losing your files to ransomware?",
                    Options = new List<string> { "Paying the ransom immediately", "Regular backups to a separate location", "Turning off your antivirus", "Opening every attachment to check it" },
                    CorrectIndex = 1,
                    Explanation = "Regular backups mean you can restore your files without paying criminals.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "It is fine to reuse the same strong password across all your accounts.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 1,
                    Explanation = "False. If one site is breached, reused passwords put all your other accounts at risk.",
                    IsTrueFalse = true
                },
                new QuizQuestion
                {
                    Question = "In South Africa, which law protects your personal information?",
                    Options = new List<string> { "POPIA", "GDPR", "HIPAA", "FICA" },
                    CorrectIndex = 0,
                    Explanation = "POPIA (Protection of Personal Information Act) governs how personal data is handled in South Africa.",
                    IsTrueFalse = false
                },
                new QuizQuestion
                {
                    Question = "A bank will sometimes ask for your full PIN or OTP over the phone to verify you.",
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = 1,
                    Explanation = "False. Banks never ask for your full PIN or OTP. Anyone who does is trying to scam you.",
                    IsTrueFalse = true
                }
            };
        }
    }
}
