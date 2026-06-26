using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CyberBot_Part3.Logic
{
    /// <summary>
    /// Simulates NLP by detecting intent from varied user phrases using
    /// keyword matching and regex patterns. Extends the Part 2 approach.
    /// </summary>
    public static class NlpEngine
    {
        // ── Intent definitions ─────────────────────────────────────────────────
        public enum Intent
        {
            Unknown,
            AddTask,
            SetReminder,
            ViewTasks,
            StartQuiz,
            ShowLog,
            DeleteTask,
            CompleteTask,
            CyberTopic,
            Greeting,
            Goodbye,
            Thanks
        }

        // Phrase variations that map to each intent
        private static readonly List<(string[] keywords, Intent intent)> IntentMap
            = new List<(string[], Intent)>
        {
            // Add Task
            (new[] { "add task", "create task", "new task", "add a task", "make a task",
                     "add reminder", "set task", "i need to", "schedule task" }, Intent.AddTask),

            // Set Reminder
            (new[] { "remind me", "set a reminder", "set reminder", "reminder for",
                     "don't let me forget", "remind me to", "can you remind" }, Intent.SetReminder),

            // View Tasks
            (new[] { "show tasks", "view tasks", "list tasks", "my tasks", "show my tasks",
                     "what tasks", "pending tasks", "all tasks", "view my tasks" }, Intent.ViewTasks),

            // Start Quiz
            (new[] { "start quiz", "begin quiz", "take quiz", "quiz me", "test me",
                     "cybersecurity quiz", "start the quiz", "play quiz", "i want to quiz" }, Intent.StartQuiz),

            // Show Activity Log
            (new[] { "show log", "activity log", "show activity", "what have you done",
                     "recent actions", "show history", "what did you do", "your actions",
                     "show me the log", "what have you done for me" }, Intent.ShowLog),

            // Delete Task
            (new[] { "delete task", "remove task", "cancel task", "get rid of task" }, Intent.DeleteTask),

            // Complete Task
            (new[] { "complete task", "mark done", "mark as done", "finished task",
                     "task done", "mark complete", "mark as complete" }, Intent.CompleteTask),

            // Greeting
            (new[] { "hello", "hi", "hey", "howdy", "good morning", "good afternoon",
                     "what's up", "sup", "greetings" }, Intent.Greeting),

            // Goodbye
            (new[] { "bye", "goodbye", "see you", "exit", "quit", "farewell" }, Intent.Goodbye),

            // Thanks
            (new[] { "thanks", "thank you", "thank you so much", "cheers", "appreciated" }, Intent.Thanks),
        };

        // Cybersecurity topic keywords — kept for topic routing
        private static readonly Dictionary<string, string> TopicKeywords
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = "password",
                ["phishing"] = "phishing",
                ["malware"] = "malware",
                ["virus"] = "malware",
                ["ransomware"] = "malware",
                ["vpn"] = "vpn",
                ["2fa"] = "2fa",
                ["two factor"] = "2fa",
                ["two-factor"] = "2fa",
                ["firewall"] = "firewall",
                ["encryption"] = "encryption",
                ["encrypt"] = "encryption",
                ["privacy"] = "privacy",
                ["scam"] = "scam",
                ["fraud"] = "scam"
            };

        // ── Public API ─────────────────────────────────────────────────────────

        public static Intent DetectIntent(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Intent.Unknown;
            string lower = input.ToLowerInvariant();

            foreach (var (keywords, intent) in IntentMap)
                foreach (string kw in keywords)
                    if (lower.Contains(kw))
                        return intent;

            // Fall back to cyber topic
            foreach (var kvp in TopicKeywords)
                if (lower.Contains(kvp.Key))
                    return Intent.CyberTopic;

            return Intent.Unknown;
        }

        public static string DetectCyberTopic(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string lower = input.ToLowerInvariant();
            foreach (var kvp in TopicKeywords)
                if (lower.Contains(kvp.Key))
                    return kvp.Value;
            return "";
        }

        /// <summary>
        /// Tries to extract a task title from free-form input.
        /// e.g. "Add a task to enable 2FA" → "Enable 2FA"
        /// </summary>
        public static string ExtractTaskTitle(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            string[] stripPrefixes =
            {
                "add a task to", "add task to", "add a task called", "add task called",
                "create a task to", "create task to", "new task to", "new task called",
                "add a reminder to", "set a task to", "i need to", "i want to"
            };

            string lower = input.ToLowerInvariant();
            foreach (string prefix in stripPrefixes)
            {
                if (lower.StartsWith(prefix))
                {
                    string remaining = input.Substring(prefix.Length).Trim();
                    return Capitalise(remaining);
                }
            }

            return Capitalise(input.Trim());
        }

        /// <summary>
        /// Tries to extract a reminder timeframe from input.
        /// e.g. "remind me in 3 days" → "3 days"
        /// </summary>
        public static string ExtractReminderText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Pattern: "in X days/hours/weeks"
            var match = Regex.Match(input, @"in\s+(\d+\s+(?:day|days|hour|hours|week|weeks))", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;

            // Pattern: "tomorrow"
            if (input.ToLowerInvariant().Contains("tomorrow"))
                return DateTime.Now.AddDays(1).ToString("dd MMM yyyy");

            // Pattern: "next week"
            if (input.ToLowerInvariant().Contains("next week"))
                return DateTime.Now.AddDays(7).ToString("dd MMM yyyy");

            // Pattern: "on [date]" 
            match = Regex.Match(input, @"on\s+(.+)$", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value.Trim();

            return "";
        }

        private static string Capitalise(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
