using System;
using System.Collections.Generic;

namespace CyberBot_Part3.Logic
{
    public class QuizQuestion
    {
        public string Question { get; set; }
        public string[] Options { get; set; }   // null = True/False question
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; }
        public bool IsTrueFalse { get; set; }
    }

    public class QuizEngine
    {
        private int _current = 0;
        private int _score = 0;
        public bool IsActive { get; private set; } = false;
        public int TotalQuestions => _questions.Count;
        public int CurrentIndex => _current;
        public int Score => _score;

        private readonly List<QuizQuestion> _questions = new List<QuizQuestion>
        {
            // ── Multiple Choice ───────────────────────────────────────────────
            new QuizQuestion
            {
                Question     = "What should you do if you receive an email asking for your password?",
                Options      = new[] { "A) Reply with your password", "B) Delete the email",
                                       "C) Report it as phishing", "D) Ignore it" },
                CorrectIndex = 2,
                Explanation  = "Reporting phishing emails helps protect others and alerts your IT/security team.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "What is the minimum recommended length for a strong password?",
                Options      = new[] { "A) 6 characters", "B) 8 characters",
                                       "C) 12 characters", "D) 4 characters" },
                CorrectIndex = 2,
                Explanation  = "Security experts recommend at least 12 characters for a strong password.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "What does 2FA stand for?",
                Options      = new[] { "A) Two-Factor Authentication", "B) Two-File Access",
                                       "C) Trusted Firewall Access", "D) Two-Frequency Alarm" },
                CorrectIndex = 0,
                Explanation  = "2FA = Two-Factor Authentication. It adds a second layer of security beyond your password.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "Which of the following is the safest way to store passwords?",
                Options      = new[] { "A) Write them in a notebook", "B) Save them in a text file",
                                       "C) Use a password manager", "D) Memorise them all" },
                CorrectIndex = 2,
                Explanation  = "Password managers securely encrypt and store all your passwords.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "What does a VPN primarily protect?",
                Options      = new[] { "A) Your device from viruses", "B) Your internet traffic from eavesdropping",
                                       "C) Your files from deletion", "D) Your screen from others" },
                CorrectIndex = 1,
                Explanation  = "A VPN encrypts your internet traffic, protecting it from ISPs and network snoopers.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "Which of these is an example of social engineering?",
                Options      = new[] { "A) Installing antivirus software", "B) A hacker guessing your password",
                                       "C) Someone calling pretending to be IT support", "D) Using a firewall" },
                CorrectIndex = 2,
                Explanation  = "Social engineering manipulates people into revealing confidential information.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "What type of malware encrypts your files and demands payment?",
                Options      = new[] { "A) Spyware", "B) Adware",
                                       "C) Ransomware", "D) Worm" },
                CorrectIndex = 2,
                Explanation  = "Ransomware encrypts victim files and demands a ransom for the decryption key.",
                IsTrueFalse  = false
            },
            new QuizQuestion
            {
                Question     = "What does HTTPS indicate about a website?",
                Options      = new[] { "A) It is free to use", "B) It has an encrypted connection",
                                       "C) It is government-approved", "D) It loads faster" },
                CorrectIndex = 1,
                Explanation  = "HTTPS means the connection is encrypted via SSL/TLS, protecting data in transit.",
                IsTrueFalse  = false
            },
            // ── True / False ──────────────────────────────────────────────────
            new QuizQuestion
            {
                Question     = "True or False: You should use the same password for all your accounts to make them easier to remember.",
                Options      = new[] { "True", "False" },
                CorrectIndex = 1,
                Explanation  = "FALSE — reusing passwords means one breach exposes ALL your accounts.",
                IsTrueFalse  = true
            },
            new QuizQuestion
            {
                Question     = "True or False: Public Wi-Fi networks are generally safe for online banking.",
                Options      = new[] { "True", "False" },
                CorrectIndex = 1,
                Explanation  = "FALSE — public Wi-Fi is unencrypted and vulnerable to eavesdropping.",
                IsTrueFalse  = true
            },
            new QuizQuestion
            {
                Question     = "True or False: A firewall can help prevent unauthorised access to your network.",
                Options      = new[] { "True", "False" },
                CorrectIndex = 0,
                Explanation  = "TRUE — firewalls monitor and filter network traffic to block threats.",
                IsTrueFalse  = true
            },
            new QuizQuestion
            {
                Question     = "True or False: Encryption makes data unreadable to anyone without the correct key.",
                Options      = new[] { "True", "False" },
                CorrectIndex = 0,
                Explanation  = "TRUE — encryption converts data into a format only decryptable with the right key.",
                IsTrueFalse  = true
            }
        };

        public void Start()
        {
            _current = 0;
            _score = 0;
            IsActive = true;
            ActivityLog.Add("Quiz", "Quiz started");
        }

        public QuizQuestion CurrentQuestion
            => (_current < _questions.Count) ? _questions[_current] : null;

        /// <summary>Submit answer (0-based index). Returns true if correct.</summary>
        public bool SubmitAnswer(int answerIndex, out string feedback)
        {
            var q = CurrentQuestion;
            if (q == null) { feedback = "No active question."; return false; }

            bool correct = (answerIndex == q.CorrectIndex);
            if (correct) _score++;

            feedback = correct
                ? $"✅ Correct! {q.Explanation}"
                : $"❌ Wrong! The correct answer was: {q.Options[q.CorrectIndex]}\n   {q.Explanation}";

            ActivityLog.Add("Quiz", $"Q{_current + 1} answered {(correct ? "correctly" : "incorrectly")}");
            _current++;

            if (_current >= _questions.Count)
            {
                IsActive = false;
                ActivityLog.Add("Quiz", $"Quiz completed — Score: {_score}/{_questions.Count}");
            }

            return correct;
        }

        public bool IsFinished => _current >= _questions.Count;

        public string FinalFeedback()
        {
            double pct = (double)_score / _questions.Count * 100;
            if (pct >= 90) return "🏆 Outstanding! You're a cybersecurity pro!";
            if (pct >= 70) return "👍 Great job! You know your cybersecurity basics well.";
            if (pct >= 50) return "📚 Not bad! Keep learning to stay safe online.";
            return "💡 Keep learning to stay safe online! Review the topics and try again.";
        }
    }
}
