using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using CyberAwarenessBot.Models;

namespace CyberAwarenessBot.Services
{
    public class TaskService
    {
        public bool UsingDatabase { get; private set; }
        public string DatabaseError { get; private set; } = string.Empty;

        // Fallback store used only when MySQL is unavailable, so the app still runs.
        private readonly List<CyberTask> memoryTasks = new List<CyberTask>();
        private int memoryNextId = 1;

        public TaskService()
        {
            UsingDatabase = DatabaseHelper.TryInitialize(out string error);
            DatabaseError = error;
        }

        public int AddTask(CyberTask task)
        {
            if (!UsingDatabase)
            {
                task.Id = memoryNextId++;
                task.CreatedAt = DateTime.Now;
                memoryTasks.Add(task);
                return task.Id;
            }

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO tasks (title, description, reminder_date, is_completed) " +
                "VALUES (@title, @description, @reminder, 0);";
            cmd.Parameters.AddWithValue("@title", task.Title);
            cmd.Parameters.AddWithValue("@description", task.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@reminder", (object?)task.ReminderDate ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            task.Id = (int)cmd.LastInsertedId;
            return task.Id;
        }

        public List<CyberTask> GetTasks()
        {
            if (!UsingDatabase)
            {
                return new List<CyberTask>(memoryTasks);
            }

            var result = new List<CyberTask>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, title, description, reminder_date, is_completed, created_at FROM tasks ORDER BY id;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var task = new CyberTask
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("description")),
                    ReminderDate = reader.IsDBNull(reader.GetOrdinal("reminder_date"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("reminder_date")),
                    IsCompleted = reader.GetInt32(reader.GetOrdinal("is_completed")) == 1,
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
                result.Add(task);
            }
            return result;
        }

        public bool MarkComplete(int id)
        {
            if (!UsingDatabase)
            {
                var t = memoryTasks.Find(x => x.Id == id);
                if (t == null) return false;
                t.IsCompleted = true;
                return true;
            }

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE tasks SET is_completed = 1 WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool SetReminder(int id, DateTime reminder)
        {
            if (!UsingDatabase)
            {
                var t = memoryTasks.Find(x => x.Id == id);
                if (t == null) return false;
                t.ReminderDate = reminder;
                return true;
            }

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE tasks SET reminder_date = @reminder WHERE id = @id;";
            cmd.Parameters.AddWithValue("@reminder", reminder);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteTask(int id)
        {
            if (!UsingDatabase)
            {
                return memoryTasks.RemoveAll(x => x.Id == id) > 0;
            }

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM tasks WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
