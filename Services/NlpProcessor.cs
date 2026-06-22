using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberAwarenessBot.Services
{
    public enum Intent
    {
        AddTask,
        ShowTasks,
        CompleteTask,
        DeleteTask,
        StartQuiz,
        ShowLog,
        None
    }

    public class NlpResult
    {
        public Intent Intent { get; set; } = Intent.None;
        public string Text { get; set; } = string.Empty;
        public DateTime? ReminderDate { get; set; }
        public int? TaskNumber { get; set; }
    }

    public class NlpProcessor
    {
        private readonly List<string> addTaskTriggers = new List<string>
        {
            "add task", "add a task", "create task", "create a task", "new task",
            "remind me to", "set a reminder to", "set reminder to", "add reminder to",
            "i need to", "remember to", "make a task", "add to my tasks", "i have to"
        };

        private readonly List<string> showTasksTriggers = new List<string>
        {
            "show tasks", "show my tasks", "my tasks", "list tasks", "view tasks",
            "see tasks", "what are my tasks", "what tasks", "show task list",
            "to do list", "to-do list", "todo list", "view my tasks", "list my tasks"
        };

        private readonly List<string> completeTriggers = new List<string>
        {
            "complete task", "mark task", "finish task", "done task", "completed task",
            "mark complete", "mark as complete", "mark as done", "complete", "finished", "i did task"
        };

        private readonly List<string> deleteTriggers = new List<string>
        {
            "delete task", "remove task", "cancel task", "delete", "remove", "get rid of task"
        };

        private readonly List<string> quizTriggers = new List<string>
        {
            "quiz", "mini game", "mini-game", "test me", "play game", "start game",
            "start quiz", "play quiz", "game", "test my knowledge"
        };

        private readonly List<string> logTriggers = new List<string>
        {
            "activity log", "show log", "show the log", "show activity", "what have you done",
            "what did you do", "history", "show history", "your actions", "recent actions",
            "what have you done for me", "show me the log"
        };

        public NlpResult Analyse(string input)
        {
            var result = new NlpResult();
            if (string.IsNullOrWhiteSpace(input)) return result;

            string lower = input.ToLower().Trim();

            // Activity log
            if (ContainsAny(lower, logTriggers))
            {
                result.Intent = Intent.ShowLog;
                return result;
            }

            // Quiz / mini-game
            if (ContainsAny(lower, quizTriggers))
            {
                result.Intent = Intent.StartQuiz;
                return result;
            }

            // Show tasks
            if (ContainsAny(lower, showTasksTriggers))
            {
                result.Intent = Intent.ShowTasks;
                return result;
            }

            // Complete a task (check before delete and before add)
            if (ContainsAny(lower, completeTriggers) && !ContainsAny(lower, addTaskTriggers))
            {
                result.Intent = Intent.CompleteTask;
                result.TaskNumber = ExtractNumber(lower);
                result.Text = StripTriggers(lower, completeTriggers);
                return result;
            }

            // Delete a task
            if (ContainsAny(lower, deleteTriggers) && !ContainsAny(lower, addTaskTriggers))
            {
                result.Intent = Intent.DeleteTask;
                result.TaskNumber = ExtractNumber(lower);
                result.Text = StripTriggers(lower, deleteTriggers);
                return result;
            }

            // Add a task (with optional reminder)
            string? matchedTrigger = FirstMatch(lower, addTaskTriggers);
            if (matchedTrigger != null)
            {
                result.Intent = Intent.AddTask;
                result.ReminderDate = ParseReminder(lower);

                string title = ExtractAfter(input, matchedTrigger);
                title = RemoveReminderClause(title);
                result.Text = Capitalise(title.Trim(" .,".ToCharArray()));
                return result;
            }

            return result;
        }

        public DateTime? ParseReminder(string input)
        {
            string lower = input.ToLower();

            if (lower.Contains("tomorrow"))
                return DateTime.Now.Date.AddDays(1).AddHours(9);

            if (lower.Contains("today") || lower.Contains("later"))
                return DateTime.Now.AddHours(3);

            if (lower.Contains("next week"))
                return DateTime.Now.Date.AddDays(7).AddHours(9);

            if (lower.Contains("next month"))
                return DateTime.Now.Date.AddMonths(1).AddHours(9);

            // "in a day", "in a week"
            if (Regex.IsMatch(lower, @"in\s+a\s+day")) return DateTime.Now.AddDays(1);
            if (Regex.IsMatch(lower, @"in\s+a\s+week")) return DateTime.Now.AddDays(7);
            if (Regex.IsMatch(lower, @"in\s+a\s+month")) return DateTime.Now.AddMonths(1);

            // "in 3 days", "in 2 weeks", "in 5 hours", "in 1 month"
            var match = Regex.Match(lower, @"in\s+(\d+)\s+(hour|hours|day|days|week|weeks|month|months)");
            if (match.Success)
            {
                int amount = int.Parse(match.Groups[1].Value);
                string unit = match.Groups[2].Value;

                return unit switch
                {
                    "hour" or "hours"   => DateTime.Now.AddHours(amount),
                    "day" or "days"     => DateTime.Now.Date.AddDays(amount).AddHours(9),
                    "week" or "weeks"   => DateTime.Now.Date.AddDays(amount * 7).AddHours(9),
                    "month" or "months" => DateTime.Now.Date.AddMonths(amount).AddHours(9),
                    _ => (DateTime?)null
                };
            }

            return null;
        }

        private string RemoveReminderClause(string text)
        {
            string result = text;
            result = Regex.Replace(result, @",?\s*remind me.*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @",?\s*and remind me.*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @",?\s*set a reminder.*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @",?\s*in\s+\d+\s+(hour|hours|day|days|week|weeks|month|months).*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @",?\s*in\s+a\s+(day|week|month).*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+tomorrow\b.*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+next\s+(week|month)\b.*$", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+today\b.*$", "", RegexOptions.IgnoreCase);
            return result;
        }

        private static bool ContainsAny(string input, List<string> triggers)
        {
            return triggers.Any(t => input.Contains(t));
        }

        private static string? FirstMatch(string input, List<string> triggers)
        {
            // Longest trigger first so "add a task" wins over a shorter accidental match.
            foreach (var t in triggers.OrderByDescending(x => x.Length))
            {
                if (input.Contains(t)) return t;
            }
            return null;
        }

        private static string StripTriggers(string input, List<string> triggers)
        {
            string result = input;
            foreach (var t in triggers.OrderByDescending(x => x.Length))
            {
                result = result.Replace(t, " ");
            }
            return result.Trim();
        }

        private static string ExtractAfter(string original, string trigger)
        {
            int idx = original.ToLower().IndexOf(trigger);
            if (idx < 0) return original;
            int start = idx + trigger.Length;
            return start < original.Length ? original.Substring(start).Trim() : string.Empty;
        }

        private static int? ExtractNumber(string input)
        {
            var match = Regex.Match(input, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int n))
            {
                return n;
            }
            return null;
        }

        private static string Capitalise(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return char.ToUpper(text[0]) + (text.Length > 1 ? text.Substring(1) : string.Empty);
        }
    }
}
