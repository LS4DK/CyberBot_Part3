using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace CyberBot_Part3.Data
{
    public class CyberTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class DatabaseManager
    {
        // ── Change these if your MySQL setup differs ───────────────────────────
        private const string Server = "localhost";
        private const string Database = "cyberbot_db";
        private const string User = "root";
        private const string Password = "root";          // set your MySQL root password here
        // ──────────────────────────────────────────────────────────────────────

        private static string ConnectionString =>
            $"Server={Server};Database={Database};User Id={User};Password={Password};";

        // ── Initialise DB + table ──────────────────────────────────────────────
        public static void Initialise()
        {
            // 1. Create the database if it doesn't exist
            string rootConn = $"Server={Server};User Id={User};Password={Password};";
            using (var conn = new MySqlConnection(rootConn))
            {
                conn.Open();
                string createDb = $"CREATE DATABASE IF NOT EXISTS `{Database}`;";
                new MySqlCommand(createDb, conn).ExecuteNonQuery();
            }

            // 2. Create the tasks table if it doesn't exist
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string createTable = @"
                    CREATE TABLE IF NOT EXISTS tasks (
                        id            INT AUTO_INCREMENT PRIMARY KEY,
                        title         VARCHAR(200)  NOT NULL,
                        description   TEXT,
                        reminder_date VARCHAR(100),
                        is_completed  TINYINT(1)    NOT NULL DEFAULT 0,
                        created_at    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );";
                new MySqlCommand(createTable, conn).ExecuteNonQuery();
            }
        }

        // ── CRUD operations ────────────────────────────────────────────────────
        public static void AddTask(CyberTask task)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO tasks (title, description, reminder_date, is_completed)
                               VALUES (@title, @desc, @reminder, 0);";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@title", task.Title);
                    cmd.Parameters.AddWithValue("@desc", task.Description ?? "");
                    cmd.Parameters.AddWithValue("@reminder", task.ReminderDate ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<CyberTask> GetAllTasks()
        {
            var list = new List<CyberTask>();
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM tasks ORDER BY created_at DESC;";
                using (var reader = new MySqlCommand(sql, conn).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CyberTask
                        {
                            Id = reader.GetInt32("id"),
                            Title = reader.GetString("title"),
                            Description = reader.GetString("description"),
                            ReminderDate = reader.GetString("reminder_date"),
                            IsCompleted = reader.GetBoolean("is_completed"),
                            CreatedAt = reader.GetDateTime("created_at")
                        });
                    }
                }
            }
            return list;
        }

        public static void MarkCompleted(int id)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "UPDATE tasks SET is_completed = 1 WHERE id = @id;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteTask(int id)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "DELETE FROM tasks WHERE id = @id;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Connection test ────────────────────────────────────────────────────
        public static bool TestConnection(out string error)
        {
            try
            {
                Initialise();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
