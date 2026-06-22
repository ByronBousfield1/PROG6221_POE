using System;

namespace CyberAwarenessBot.Models
{
    public class CyberTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ReminderDisplay =>
            ReminderDate.HasValue ? ReminderDate.Value.ToString("ddd dd MMM yyyy, HH:mm") : "No reminder";

        public string StatusDisplay => IsCompleted ? "Completed" : "Pending";

        public bool IsReminderDue =>
            !IsCompleted && ReminderDate.HasValue && ReminderDate.Value <= DateTime.Now;
    }
}
