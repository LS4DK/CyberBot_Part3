using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CyberBot_Part3.Data;
using CyberBot_Part3.Logic;

namespace CyberBot_Part3.Logic
{
    public class ChatbotEngine
    {
        private string _lastTopic = "";
        private readonly Random _rng = new Random();
        public UserMemory Memory { get; } = new UserMemory();
        public QuizEngine Quiz { get; } = new QuizEngine();

        // ── State flags ────────────────────────────────────────────────────────
        private bool _awaitingReminderForTask = false;
        private string _pendingTaskTitle = "";
        private bool _awaitingQuizAnswer = false;

        // ── Random tip pools ───────────────────────────────────────────────────
        private static readonly Dictionary<string, List<string>> RandomPools =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["phishing"] = new List<string>
                {
                    "Be cautious of emails asking for personal information — scammers often disguise themselves as trusted organisations.",
                    "Always hover over links before clicking to see where they really lead.",
                    "Legitimate companies will never ask for your password via email.",
                    "Look for spelling mistakes and mismatched sender addresses in emails.",
                    "When in doubt, go directly to the company website instead of clicking links."
                },
                ["password"] = new List<string>
                {
                    "Use at least 12 characters with a mix of letters, numbers and symbols.",
                    "Consider using a passphrase — four random words strung together are very strong.",
                    "Never reuse the same password across multiple websites.",
                    "A password manager can generate and store strong, unique passwords for you.",
                    "Avoid using personal details like your name or birthday in passwords."
                },
                ["scam"] = new List<string>
                {
                    "If an offer sounds too good to be true, it almost certainly is.",
                    "Never transfer money to someone you have only met online.",
                    "Government agencies will never demand immediate payment by gift card.",
                    "Verify unexpected prize or lottery wins before taking any action.",
                    "Report scams to your national cybercrime authority to protect others."
                },
                ["malware"] = new List<string>
                {
                    "Keep your operating system and all software up to date to patch known vulnerabilities.",
                    "Only download software from official or well-known sources.",
                    "Run a reputable antivirus/antimalware tool and keep its definitions current.",
                    "Be wary of USB drives from unknown sources — they can carry malware.",
                    "Ransomware spreads via email attachments — never open files you weren't expecting."
                }
            };

        private static readonly HashSet<string> ReferenceTokens =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "that","this","it","more","again","same","another",
                "details","continue","repeat","else","further","explain",
                "elaborate","tell"
            };

        private static readonly Dictionary<Sentiment, string> SentimentTips =
            new Dictionary<Sentiment, string>
            {
                [Sentiment.Worried] = "\n💡 Tip: Start with one small step — even changing one password today makes you safer.",
                [Sentiment.Frustrated] = "\n💡 Tip: Cybersecurity doesn't have to be perfect. Small improvements add up.",
                [Sentiment.Curious] = "\n💡 Tip: Curiosity is your best defence. Keep asking questions!",
                [Sentiment.Happy] = "",
                [Sentiment.Neutral] = ""
            };

        // ══════════════════════════════════════════════════════════════════════
        //  MAIN ENTRY POINT
        // ══════════════════════════════════════════════════════════════════════
        public string GetResponse(string input, string personality)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Please type something so I can help you.";

            Sentiment sentiment = SentimentDetector.Detect(input);
            string empathy = SentimentDetector.EmpathyPrefix(sentiment);
            string sentimentTip = SentimentTips.TryGetValue(sentiment, out string st) ? st : "";

            string normalized = input.ToLowerInvariant();
            string[] tokens = Regex.Split(normalized, @"\W+")
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToArray();

            // ── 1. Handle pending quiz answer ──────────────────────────────────
            if (_awaitingQuizAnswer && Quiz.IsActive)
            {
                return HandleQuizAnswer(input);
            }

            // ── 2. Handle pending reminder confirmation ────────────────────────
            if (_awaitingReminderForTask)
            {
                return HandleReminderReply(input);
            }

            // ── 3. Detect NLP intent ───────────────────────────────────────────
            NlpEngine.Intent intent = NlpEngine.DetectIntent(input);
            ActivityLog.Add("NLP", $"Detected intent: {intent} from: \"{input}\"");

            switch (intent)
            {
                case NlpEngine.Intent.AddTask:
                    return HandleAddTask(input, personality);

                case NlpEngine.Intent.SetReminder:
                    return HandleSetReminder(input, personality);

                case NlpEngine.Intent.ViewTasks:
                    return HandleViewTasks();

                case NlpEngine.Intent.StartQuiz:
                    return HandleStartQuiz();

                case NlpEngine.Intent.ShowLog:
                    return HandleShowLog();

                case NlpEngine.Intent.DeleteTask:
                    return "To delete a task, please use the 📋 Tasks tab and click 'Delete' next to the task.";

                case NlpEngine.Intent.CompleteTask:
                    return "To mark a task complete, please use the 📋 Tasks tab and click 'Mark Done'.";

                case NlpEngine.Intent.Greeting:
                    string n = string.IsNullOrEmpty(Memory.Name) ? "there" : Memory.Name;
                    ActivityLog.Add("Chat", $"Greeting from {n}");
                    return empathy + Speak($"Hey {n}! What cybersecurity topic can I help you with today?", personality);

                case NlpEngine.Intent.Goodbye:
                    return empathy + Speak($"Goodbye, {Memory.Name ?? "there"}! Stay safe online. 👋", personality);

                case NlpEngine.Intent.Thanks:
                    return empathy + Speak($"You're welcome, {Memory.Name ?? "there"}! Always here to help.", personality);

                case NlpEngine.Intent.CyberTopic:
                    break; // Fall through to topic handling below

                default:
                    break;
            }

            // ── 4. Memory extraction ───────────────────────────────────────────
            string memoryReply = TryExtractMemory(normalized, tokens);
            if (memoryReply != null)
            {
                ActivityLog.Add("Chat", $"Memory updated: {memoryReply.Substring(0, Math.Min(50, memoryReply.Length))}");
                return empathy + memoryReply;
            }

            // ── 5. Special commands ────────────────────────────────────────────
            string special = HandleSpecial(normalized, tokens, personality);
            if (special != null) return empathy + special;

            // ── 6. Cyber topic ─────────────────────────────────────────────────
            string detectedTopic = NlpEngine.DetectCyberTopic(normalized);
            bool referencesPrev = tokens.Intersect(ReferenceTokens, StringComparer.OrdinalIgnoreCase).Any();

            string topic;
            if (!string.IsNullOrEmpty(detectedTopic))
            {
                topic = detectedTopic;
                _lastTopic = topic;
            }
            else if (referencesPrev && !string.IsNullOrEmpty(_lastTopic))
            {
                topic = _lastTopic;
            }
            else
            {
                ActivityLog.Add("Chat", "Unknown input — fallback response");
                return empathy
                     + "I didn't quite understand that. Could you rephrase?\n"
                     + "You can ask about: passwords, phishing, malware, VPN, 2FA, firewalls, encryption, scams, or privacy.\n"
                     + "Or try: 'start quiz', 'add task', 'show tasks', 'show activity log'."
                     + sentimentTip;
            }

            bool isHow = tokens.Contains("how");
            bool isWhy = tokens.Contains("why");
            bool isWhat = tokens.Contains("what");
            bool isWhen = tokens.Contains("when");
            bool wantsTip = normalized.Contains("tip") || normalized.Contains("advice")
                         || normalized.Contains("random") || normalized.Contains("give me");

            string hint = Memory.PersonalisedHint(topic);
            string body = BuildTopicResponse(topic, isHow, isWhy, isWhat, isWhen, wantsTip, personality);

            ActivityLog.Add("Chat", $"Responded to topic: {topic}");
            return empathy + hint + body + sentimentTip;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TASK HANDLERS
        // ══════════════════════════════════════════════════════════════════════
        private string HandleAddTask(string input, string personality)
        {
            string title = NlpEngine.ExtractTaskTitle(input);

            // Try to extract reminder from same sentence
            string reminder = NlpEngine.ExtractReminderText(input);

            var task = new CyberTask
            {
                Title = title,
                Description = GenerateTaskDescription(title),
                ReminderDate = reminder
            };

            try
            {
                DatabaseManager.AddTask(task);
                ActivityLog.Add("Task", $"Task added: '{title}'" + (string.IsNullOrEmpty(reminder) ? "" : $" (Reminder: {reminder})"));

                if (!string.IsNullOrEmpty(reminder))
                {
                    return Speak($"Task added: '{title}' with reminder set for {reminder}. ✅\n"
                               + "You can view all your tasks in the 📋 Tasks tab.", personality);
                }
                else
                {
                    _pendingTaskTitle = title;
                    _awaitingReminderForTask = true;
                    return Speak($"Task added: '{title}'. ✅\nWould you like a reminder for this task? (e.g., 'Yes, in 3 days' or 'No')", personality);
                }
            }
            catch (Exception ex)
            {
                return $"⚠️ Could not save task to database: {ex.Message}\nPlease check your MySQL connection in the Settings tab.";
            }
        }

        private string HandleReminderReply(string input)
        {
            _awaitingReminderForTask = false;
            string lower = input.ToLowerInvariant();

            if (lower.Contains("no") || lower.Contains("skip") || lower.Contains("later"))
            {
                _pendingTaskTitle = "";
                return "No reminder set. You can manage your tasks in the 📋 Tasks tab anytime.";
            }

            string reminder = NlpEngine.ExtractReminderText(input);
            if (string.IsNullOrEmpty(reminder)) reminder = input.Trim();

            try
            {
                // Update most recent task with reminder
                var tasks = DatabaseManager.GetAllTasks();
                var target = tasks.FirstOrDefault(t => t.Title == _pendingTaskTitle);
                if (target != null)
                {
                    // Re-add with reminder (simple approach for demonstration)
                    DatabaseManager.DeleteTask(target.Id);
                    target.ReminderDate = reminder;
                    DatabaseManager.AddTask(target);
                    ActivityLog.Add("Reminder", $"Reminder set for '{_pendingTaskTitle}': {reminder}");
                }
                _pendingTaskTitle = "";
                return $"Got it! I'll remind you about '{(target?.Title ?? "the task")}' in {reminder}. ⏰";
            }
            catch
            {
                _pendingTaskTitle = "";
                return $"Reminder noted: {reminder}. ⏰";
            }
        }

        private string HandleSetReminder(string input, string personality)
        {
            string reminder = NlpEngine.ExtractReminderText(input);
            string taskHint = NlpEngine.ExtractTaskTitle(input);

            if (string.IsNullOrEmpty(reminder))
                return Speak("When would you like to be reminded? (e.g., 'in 3 days', 'tomorrow', 'next week')", personality);

            ActivityLog.Add("Reminder", $"Reminder set: '{taskHint}' → {reminder}");
            return Speak($"Reminder set for '{taskHint}' on {reminder}. ⏰", personality);
        }

        private string HandleViewTasks()
        {
            try
            {
                var tasks = DatabaseManager.GetAllTasks();
                if (tasks.Count == 0)
                    return "You have no tasks yet. Try saying 'Add a task to enable 2FA'!";

                var sb = new StringBuilder("📋 Here are your current tasks:\n\n");
                int pending = 0;
                int completed = 0;

                foreach (var t in tasks)
                {
                    string status = t.IsCompleted ? "✅" : "⏳";
                    sb.AppendLine($"{status} [{t.Id}] {t.Title}");
                    sb.AppendLine($"     📝 {t.Description}");
                    if (!string.IsNullOrEmpty(t.ReminderDate))
                        sb.AppendLine($"     ⏰ Reminder: {t.ReminderDate}");
                    sb.AppendLine();

                    if (t.IsCompleted) completed++; else pending++;
                }

                sb.AppendLine($"Total: {tasks.Count} | Pending: {pending} | Completed: {completed}");
                sb.AppendLine("Tip: Use the 📋 Tasks tab to manage, complete, or delete tasks.");

                ActivityLog.Add("Task", $"Viewed {tasks.Count} tasks");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"⚠️ Could not load tasks: {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  QUIZ HANDLERS
        // ══════════════════════════════════════════════════════════════════════
        private string HandleStartQuiz()
        {
            Quiz.Start();
            _awaitingQuizAnswer = true;
            var q = Quiz.CurrentQuestion;
            var sb = new StringBuilder();
            sb.AppendLine("🎮 Cybersecurity Quiz Started! Answer each question to test your knowledge.\n");
            sb.AppendLine($"Question 1/{Quiz.TotalQuestions}:");
            sb.AppendLine(q.Question);
            sb.AppendLine();
            for (int i = 0; i < q.Options.Length; i++)
                sb.AppendLine(q.Options[i]);
            sb.AppendLine("\nType the letter of your answer (A/B/C/D) or True/False:");
            return sb.ToString();
        }

        private string HandleQuizAnswer(string input)
        {
            var q = Quiz.CurrentQuestion;
            if (q == null) { _awaitingQuizAnswer = false; return "Quiz is not active. Type 'start quiz' to begin!"; }

            int answerIndex = ParseAnswer(input.Trim(), q);
            if (answerIndex < 0)
                return "Please enter a valid answer: A, B, C, D (or True/False for T/F questions).";

            string feedback;
            Quiz.SubmitAnswer(answerIndex, out feedback);

            var sb = new StringBuilder();
            sb.AppendLine(feedback);

            if (Quiz.IsFinished)
            {
                _awaitingQuizAnswer = false;
                sb.AppendLine($"\n🏁 Quiz Complete! Your score: {Quiz.Score}/{Quiz.TotalQuestions}");
                sb.AppendLine(Quiz.FinalFeedback());
                sb.AppendLine("\nType 'start quiz' to play again!");
            }
            else
            {
                var next = Quiz.CurrentQuestion;
                sb.AppendLine($"\n─────────────────────────────");
                sb.AppendLine($"Question {Quiz.CurrentIndex + 1}/{Quiz.TotalQuestions}:");
                sb.AppendLine(next.Question);
                sb.AppendLine();
                for (int i = 0; i < next.Options.Length; i++)
                    sb.AppendLine(next.Options[i]);
                sb.AppendLine("\nYour answer:");
            }

            return sb.ToString();
        }

        private static int ParseAnswer(string input, QuizQuestion q)
        {
            string lower = input.ToLowerInvariant().Trim();

            if (q.IsTrueFalse)
            {
                if (lower == "true" || lower == "t" || lower == "a") return 0;
                if (lower == "false" || lower == "f" || lower == "b") return 1;
                return -1;
            }

            if (lower == "a") return 0;
            if (lower == "b") return 1;
            if (lower == "c") return 2;
            if (lower == "d") return 3;
            if (int.TryParse(lower, out int num) && num >= 1 && num <= q.Options.Length)
                return num - 1;
            return -1;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVITY LOG HANDLER
        // ══════════════════════════════════════════════════════════════════════
        private static string HandleShowLog()
        {
            var entries = ActivityLog.GetRecent(10);
            if (entries.Count == 0)
                return "No activity recorded yet. Start chatting, add tasks, or take the quiz!";

            var sb = new StringBuilder("📋 Here's a summary of recent actions:\n\n");
            int num = 1;
            foreach (var entry in entries)
            {
                sb.AppendLine($"{num}. [{entry.Category}] {entry.Description}");
                sb.AppendLine($"   🕐 {entry.Timestamp:HH:mm:ss}");
                num++;
            }

            if (ActivityLog.TotalCount > 10)
                sb.AppendLine($"\n...and {ActivityLog.TotalCount - 10} more. Use the 📊 Log tab to see all.");

            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MEMORY + SPECIAL HANDLERS (carried over from Part 2)
        // ══════════════════════════════════════════════════════════════════════
        private string TryExtractMemory(string normalized, string[] tokens)
        {
            string[] topics = { "password", "phishing", "malware", "vpn", "2fa", "firewall", "encryption", "privacy", "scam" };

            if (normalized.Contains("interested in") || normalized.Contains("my favourite") || normalized.Contains("i like"))
            {
                foreach (string t in topics)
                {
                    if (normalized.Contains(t))
                    {
                        Memory.FavouriteTopic = t;
                        return $"Got it! I'll remember that you're interested in {t}. "
                             + $"As someone interested in {t}, ask me for some tips on it!";
                    }
                }
            }

            if (normalized.Contains("my name is"))
            {
                int idx = normalized.IndexOf("my name is") + "my name is".Length;
                string extracted = normalized.Substring(idx).Trim().Split(' ')[0];
                if (!string.IsNullOrEmpty(extracted))
                {
                    Memory.Name = Capitalise(extracted);
                    return $"Nice to meet you, {Memory.Name}! I'll remember your name.";
                }
            }

            return null;
        }

        private string HandleSpecial(string normalized, string[] tokens, string personality)
        {
            string n = string.IsNullOrEmpty(Memory.Name) ? "there" : Memory.Name;

            if (normalized.Contains("how are you"))
                return Speak("I'm doing great, ready to help keep you safe online!", personality);
            if (normalized.Contains("what is your purpose") || normalized.Contains("what do you do"))
                return Speak("I'm your Cybersecurity Awareness Bot — here to educate and protect you online. I can also manage your cybersecurity tasks and quiz you!", personality);
            if (normalized.Contains("who are you") || normalized.Contains("your name"))
                return Speak("I'm CyberBot v3, your personal cybersecurity guide!", personality);
            if (normalized.Contains("remember") && normalized.Contains("about me"))
            {
                string recallName = string.IsNullOrEmpty(Memory.Name) ? "not set" : Memory.Name;
                string recallTopic = string.IsNullOrEmpty(Memory.FavouriteTopic) ? "not set" : Memory.FavouriteTopic;
                return $"Here's what I remember about you:\n• Name: {recallName}\n• Favourite topic: {recallTopic}";
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TOPIC RESPONSES (same as Part 2 + new helper)
        // ══════════════════════════════════════════════════════════════════════
        private string BuildTopicResponse(string topic, bool isHow, bool isWhy, bool isWhat,
                                          bool isWhen, bool wantsTip, string personality)
        {
            if (wantsTip && RandomPools.ContainsKey(topic))
                return PickRandom(topic) + "\n\n💬 Say 'give me another tip' for a different one!";

            switch (topic)
            {
                case "password":
                    if (isHow) return Speak("Use a mix of uppercase, lowercase, numbers and symbols. Aim for 12+ characters or use a passphrase.", personality);
                    if (isWhy) return Speak("Strong passwords prevent brute-force attacks and stop hackers getting into your accounts.", personality);
                    if (isWhat) return Speak("A password is a secret credential used to verify your identity when logging into a service.", personality);
                    if (isWhen) return Speak("Change your passwords immediately after a suspected breach and every 3–6 months for important accounts.", personality);
                    return Speak("Passwords are your first line of defence. " + PickRandom("password"), personality)
                         + "\n💬 Try asking: how should I create a password? | why are passwords important?";

                case "phishing":
                    if (isHow) return Speak("Verify sender addresses, hover over links before clicking, and never open unexpected attachments.", personality);
                    if (isWhy) return Speak("Phishing tricks you into handing over credentials or installing malware, leading to identity theft.", personality);
                    if (isWhat) return Speak("Phishing is a fraudulent attack that impersonates trusted parties to steal sensitive information.", personality);
                    return Speak(PickRandom("phishing"), personality)
                         + "\n💬 Try asking: what is phishing? | how do I avoid phishing?";

                case "malware":
                    if (isHow) return Speak("Install reputable antivirus software, keep everything updated, and avoid unknown downloads.", personality);
                    if (isWhy) return Speak("Malware can steal data, encrypt files for ransom, or give attackers full control of your device.", personality);
                    if (isWhat) return Speak("Malware is malicious software designed to harm your system — including viruses, trojans and ransomware.", personality);
                    if (isWhen) return Speak("Malware most often enters through email attachments, fake downloads, and malicious websites.", personality);
                    return Speak(PickRandom("malware"), personality)
                         + "\n💬 Try asking: what is malware? | how do I protect against malware?";

                case "vpn":
                    if (isHow) return Speak("Download a reputable VPN app, connect before using public Wi-Fi, and keep it on when browsing.", personality);
                    if (isWhy) return Speak("A VPN hides your activity from your ISP and anyone else on the same network.", personality);
                    if (isWhat) return Speak("A VPN (Virtual Private Network) creates an encrypted tunnel for your internet traffic.", personality);
                    return Speak("A VPN encrypts your network traffic — especially important on public Wi-Fi.", personality)
                         + "\n💬 Try: what is a VPN? | why should I use a VPN?";

                case "2fa":
                    if (isHow) return Speak("Go to your account's security settings and enable 2FA. Use an authenticator app like Google Authenticator.", personality);
                    if (isWhy) return Speak("2FA means an attacker who steals your password still can't log in without the second factor.", personality);
                    if (isWhat) return Speak("Two-factor authentication requires a second proof of identity — usually a code — in addition to your password.", personality);
                    return Speak("2FA is one of the most effective security measures available. Enable it on every account that supports it.", personality)
                         + "\n💬 Try: what is 2FA? | how do I set up 2FA?";

                case "firewall":
                    if (isHow) return Speak("Keep your firewall enabled and configure rules to block unwanted traffic.", personality);
                    if (isWhy) return Speak("A firewall blocks unauthorised connections and prevents malicious traffic from reaching your device.", personality);
                    if (isWhat) return Speak("A firewall monitors and controls incoming and outgoing network traffic based on security rules.", personality);
                    return Speak("Firewalls are your network's gatekeeper. Keep yours on at all times.", personality)
                         + "\n💬 Try: what is a firewall? | why should I use a firewall?";

                case "encryption":
                    if (isHow) return Speak("Use services with end-to-end encryption (e.g. Signal) and enable full-disk encryption on your device.", personality);
                    if (isWhy) return Speak("Encryption ensures that even if data is intercepted, it cannot be read without the decryption key.", personality);
                    if (isWhat) return Speak("Encryption converts readable data into a scrambled format decodable only with the correct key.", personality);
                    return Speak("Encryption is the backbone of online security — it protects your data in transit and at rest.", personality)
                         + "\n💬 Try: what is encryption? | why is encryption important?";

                case "privacy":
                    if (isHow) return Speak("Limit what you share online, review app permissions regularly, and use privacy-focused browsers.", personality);
                    if (isWhy) return Speak("Protecting your privacy reduces the risk of identity theft, targeted scams and unwanted data profiling.", personality);
                    return Speak("Your personal data is valuable. Review privacy settings on all your accounts and only share what's necessary.", personality)
                         + "\n💬 Try: how do I protect my privacy? | why does privacy matter?";

                case "scam":
                    if (isHow) return Speak("Verify requests independently, never share credentials, and be sceptical of urgent offers.", personality);
                    if (isWhy) return Speak("Scammers exploit trust and urgency to steal money or personal data from victims.", personality);
                    return Speak(PickRandom("scam"), personality)
                         + "\n💬 Try: how do I avoid scams? | why are scams dangerous?";

                default:
                    return "I'm not sure I understand. You can ask about: passwords, phishing, malware, VPN, 2FA, firewalls, encryption, scams, or privacy.";
            }
        }

        // ── Helper: generate a cybersecurity description for a task title ──────
        private static string GenerateTaskDescription(string title)
        {
            string lower = title.ToLowerInvariant();
            if (lower.Contains("password")) return "Update your password to a strong, unique one with 12+ characters.";
            if (lower.Contains("2fa") || lower.Contains("two factor")) return "Enable two-factor authentication to add an extra layer of security.";
            if (lower.Contains("vpn")) return "Set up and use a VPN to protect your internet traffic.";
            if (lower.Contains("privacy")) return "Review and update your privacy settings to protect your personal data.";
            if (lower.Contains("backup")) return "Create a backup of your important data to prevent data loss.";
            if (lower.Contains("antivirus")) return "Install or update your antivirus software to protect against malware.";
            if (lower.Contains("firewall")) return "Check that your firewall is enabled and properly configured.";
            if (lower.Contains("update") || lower.Contains("patch")) return "Apply the latest security updates and patches.";
            return $"Complete the cybersecurity task: {title}";
        }

        private string PickRandom(string topic)
        {
            if (!RandomPools.TryGetValue(topic, out List<string> pool) || pool.Count == 0) return "";
            return pool[_rng.Next(pool.Count)];
        }

        private static string Speak(string body, string personality)
        {
            switch (personality)
            {
                case "professional": return body;
                case "ai": return "[CYBERBOT-AI] " + body;
                case "casual": return "Hey! " + body;
                default: return "😊 " + body;
            }
        }

        private static string Capitalise(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
