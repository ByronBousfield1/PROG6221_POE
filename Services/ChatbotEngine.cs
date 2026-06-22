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
        private readonly NlpProcessor nlp;
        private readonly TaskService taskService;
        private readonly QuizService quizService;
        private readonly ActivityLogger logger;

        public UserMemory Memory { get; }
        public Intent LastIntent { get; private set; } = Intent.None;

        private readonly Dictionary<string, string> lastResponsePerTopic = new();

        // Used for the "Would you like a reminder?" follow-up flow.
        private int awaitingReminderForTaskId = -1;
        private string awaitingReminderTaskTitle = string.Empty;

        public ChatbotEngine(UserMemory memory, TaskService taskService, QuizService quizService, ActivityLogger logger)
        {
            Memory = memory;
            this.taskService = taskService;
            this.quizService = quizService;
            this.logger = logger;

            responder = new KeywordResponder();
            sentimentDetector = new SentimentDetector();
            nlp = new NlpProcessor();

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
            LastIntent = Intent.None;

            if (string.IsNullOrWhiteSpace(input))
            {
                return "I didn't catch that. You can ask about cybersecurity topics, manage tasks, start the quiz, or view your activity log.";
            }

            // Handle the pending reminder question first.
            if (awaitingReminderForTaskId != -1)
            {
                string reminderReply = HandlePendingReminder(input);
                if (reminderReply.Length > 0) return reminderReply;
            }

            // Check for a command intent first
            var nlpResult = nlp.Analyse(input);
            if (nlpResult.Intent != Intent.None)
            {
                LastIntent = nlpResult.Intent;
                string? intentReply = HandleIntent(nlpResult);
                if (!string.IsNullOrEmpty(intentReply)) return intentReply;
            }

            // Detect mood
            string? sentiment = sentimentDetector.Detect(input);
            if (sentiment != null) Memory.LastSentiment = sentiment;

            // Fall back to the topic response strategies
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

            return "I'm not sure I understand. Try rephrasing, or ask about phishing, passwords, your tasks, or the quiz.";
        }

        private string? HandleIntent(NlpResult result)
        {
            switch (result.Intent)
            {
                case Intent.AddTask:
                    return HandleAddTask(result);

                case Intent.ShowTasks:
                    return HandleShowTasks();

                case Intent.CompleteTask:
                    return HandleCompleteTask(result);

                case Intent.DeleteTask:
                    return HandleDeleteTask(result);

                case Intent.StartQuiz:
                    quizService.Restart();
                    logger.Log("Quiz started");
                    return "Starting the cybersecurity quiz. Head to the Quiz tab to answer the questions.";

                case Intent.ShowLog:
                    return HandleShowLog();

                default:
                    return null;
            }
        }

        private string HandleAddTask(NlpResult result)
        {
            string title = string.IsNullOrWhiteSpace(result.Text) ? "Untitled task" : result.Text;

            var task = new CyberTask
            {
                Title = title,
                Description = $"Cybersecurity task: {title}.",
                ReminderDate = result.ReminderDate
            };

            int id = taskService.AddTask(task);
            logger.Log($"Task added: '{title}'" + (result.ReminderDate.HasValue
                ? $" (reminder {result.ReminderDate.Value:dd MMM HH:mm})"
                : string.Empty));

            if (result.ReminderDate.HasValue)
            {
                return $"Task added: '{title}'. Reminder set for {result.ReminderDate.Value:ddd dd MMM yyyy, HH:mm}.";
            }

            // No reminder given, ask the user (conversational follow-up).
            awaitingReminderForTaskId = id;
            awaitingReminderTaskTitle = title;
            return $"Task added: '{title}'. Would you like a reminder? You can say something like 'remind me in 3 days', or 'no'.";
        }

        private string HandlePendingReminder(string input)
        {
            string lower = input.ToLower().Trim();

            if (lower.StartsWith("no") || lower.Contains("no thanks") || lower.Contains("don't") || lower.Contains("dont"))
            {
                string title = awaitingReminderTaskTitle;
                awaitingReminderForTaskId = -1;
                awaitingReminderTaskTitle = string.Empty;
                return $"No problem, no reminder set for '{title}'.";
            }

            DateTime? reminder = nlp.ParseReminder(lower);
            if (reminder.HasValue)
            {
                taskService.SetReminder(awaitingReminderForTaskId, reminder.Value);
                logger.Log($"Reminder set for '{awaitingReminderTaskTitle}' on {reminder.Value:dd MMM HH:mm}");
                string title = awaitingReminderTaskTitle;
                awaitingReminderForTaskId = -1;
                awaitingReminderTaskTitle = string.Empty;
                return $"Reminder set for '{title}' on {reminder.Value:ddd dd MMM yyyy, HH:mm}.";
            }

            if (lower.Contains("yes"))
            {
                return "Sure. When should I remind you? For example, 'in 3 days', 'tomorrow', or 'next week'.";
            }

            // Anything unrelated cancels the wait so the user is never stuck.
            awaitingReminderForTaskId = -1;
            awaitingReminderTaskTitle = string.Empty;
            return string.Empty;
        }

        private string HandleShowTasks()
        {
            var tasks = taskService.GetTasks();
            if (tasks.Count == 0)
            {
                return "You have no tasks yet. Try saying 'add task enable two-factor authentication'.";
            }

            var lines = tasks.Select(t =>
                $"  {t.Id}. {t.Title} [{t.StatusDisplay}]" +
                (t.ReminderDate.HasValue ? $" - reminder {t.ReminderDate.Value:dd MMM HH:mm}" : string.Empty));

            return "Here are your tasks:\n" + string.Join("\n", lines) +
                   "\n\nYou can say 'complete task 1' or 'delete task 2', or use the Tasks tab.";
        }

        private string HandleCompleteTask(NlpResult result)
        {
            var tasks = taskService.GetTasks();
            if (tasks.Count == 0) return "You have no tasks to complete yet.";

            int? id = ResolveTaskId(result, tasks);
            if (id == null)
            {
                return "Which task? Tell me the task number, for example 'complete task 1'.";
            }

            var task = tasks.FirstOrDefault(t => t.Id == id.Value);
            if (task == null) return $"I couldn't find task number {id.Value}.";

            taskService.MarkComplete(id.Value);
            logger.Log($"Task completed: '{task.Title}'");
            return $"Marked '{task.Title}' as completed.";
        }

        private string HandleDeleteTask(NlpResult result)
        {
            var tasks = taskService.GetTasks();
            if (tasks.Count == 0) return "You have no tasks to delete.";

            int? id = ResolveTaskId(result, tasks);
            if (id == null)
            {
                return "Which task? Tell me the task number, for example 'delete task 2'.";
            }

            var task = tasks.FirstOrDefault(t => t.Id == id.Value);
            if (task == null) return $"I couldn't find task number {id.Value}.";

            taskService.DeleteTask(id.Value);
            logger.Log($"Task deleted: '{task.Title}'");
            return $"Deleted '{task.Title}'.";
        }

        private int? ResolveTaskId(NlpResult result, List<CyberTask> tasks)
        {
            if (result.TaskNumber.HasValue)
            {
                return result.TaskNumber.Value;
            }

            // Try to match by keyword in the title.
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                string text = result.Text.ToLower();
                var match = tasks.FirstOrDefault(t =>
                    text.Contains(t.Title.ToLower()) || t.Title.ToLower().Contains(text));
                if (match != null) return match.Id;
            }
            return null;
        }

        private string HandleShowLog()
        {
            var recent = logger.GetRecent(10);
            if (recent.Count == 0)
            {
                return "Nothing in the activity log yet. Once you add tasks or take the quiz, your actions will appear here.";
            }

            var lines = recent.Select((e, i) => $"  {i + 1}. {e.Description} ({e.Timestamp:HH:mm:ss})");
            return "Here's a summary of recent actions:\n" + string.Join("\n", lines) +
                   "\n\nThe Activity Log tab shows the full history.";
        }

        private string? TryHandleExit(string input)
        {
            string lower = input.ToLower();
            if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("bye") || lower.Contains("goodbye"))
            {
                return $"Stay safe online, {Memory.UserName}. Remember: when in doubt, don't click. Goodbye.";
            }
            return null;
        }

        private string? TryHandleSmallTalk(string input)
        {
            string lower = input.ToLower();

            if (lower.Contains("how are you"))
                return "I'm running well and ready to help you stay safe online. How can I help today?";

            if (lower.Contains("purpose") || lower.Contains("who are you") || lower.Contains("what are you"))
                return "I'm the Cybersecurity Awareness Bot. I help South African citizens recognise cyber threats, manage security tasks, and test their knowledge.";

            if (lower.StartsWith("hi") || lower.StartsWith("hello") || lower.StartsWith("hey") || lower.Contains("good morning") || lower.Contains("good afternoon"))
                return $"Hello again, {Memory.UserName}. You can ask about a topic, manage tasks, start the quiz, or view your activity log.";

            if (lower.Contains("thank"))
                return "You're welcome. Anything else I can help with?";

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
                if (lower.Contains(trigger))
                {
                    string? topic = responder.DetectTopic(input);
                    if (topic != null)
                    {
                        Memory.AddInterest(topic);
                        Memory.FavouriteTopic ??= topic;
                        Memory.RememberTopic(topic);

                        string tip = responder.GetRandomResponse(topic);
                        lastResponsePerTopic[topic] = tip;

                        return $"Got it, {Memory.UserName}. I'll remember that you're interested in {topic}. It's a crucial part of staying safe online.\n\nHere's something useful:\n{tip}";
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
                    return $"So far I know your name is {Memory.UserName}, but you haven't told me your favourite cybersecurity topic yet. Try saying 'I'm interested in privacy'.";
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
            if (lower.Contains("help") || lower.Contains("what can i ask") || lower.Contains("what can you do"))
            {
                string topics = string.Join(", ", responder.AllTopics);
                return "Here's what I can do:\n" +
                       $"  - Answer questions on: {topics}\n" +
                       "  - Manage tasks: 'add task enable 2FA, remind me in 7 days'\n" +
                       "  - Show tasks: 'show my tasks'\n" +
                       "  - Complete or delete: 'complete task 1', 'delete task 2'\n" +
                       "  - Quiz: 'start the quiz'\n" +
                       "  - Activity log: 'what have you done for me?'";
            }
            return null;
        }

        private string? FallbackResponse(string input)
        {
            string[] fallbacks =
            {
                "I'm not sure I understand. You can ask about a cybersecurity topic, manage tasks, start the quiz, or view your activity log.",
                $"I didn't catch that, {Memory.UserName}. Try 'help' to see what I can do.",
                "That's outside what I handle. Try asking about phishing, passwords, your tasks, or the quiz."
            };
            var rng = new Random();
            return fallbacks[rng.Next(fallbacks.Length)];
        }
    }
}
